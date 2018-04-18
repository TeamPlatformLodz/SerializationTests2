using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Reflection;
using System.IO;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Linq;

namespace cokolwiek
{
    class CustomFormatter : IFormatter
    {
        SerializationBinder binder;
        StreamingContext context;
        ISurrogateSelector surrogateSelector;
        ObjectIDGenerator idGenerator;

        private readonly string indenter = "    ";

        public CustomFormatter()
        {
            context = new StreamingContext(StreamingContextStates.All);
            idGenerator = new ObjectIDGenerator();
        }

        public object Deserialize(System.IO.Stream serializationStream)
        {
            using (StreamReader sr = new StreamReader(serializationStream))
            {
                /*
                 * poki nie koniec
                 * czytaj wiersz
                 * sprobuj znalezc typ obiektu
                 * jesli typ rozpoznany
                 *      stworz pusty
                 *      przejdz po props
                 *      jesli value type
                 *          ustaw
                 *       jesli REF_ID
                 *          zapisz,ze ref
                 *      jesli ID
                 *          buduj ten obiekt
                 */
                Dictionary<object, List<ClassLoadInfo>> objectsInfo = new Dictionary<object, List<ClassLoadInfo>>();
                Dictionary<string, object> objectDictionary = new Dictionary<string, object>();
                Stack<object> objectLoadOrder = new Stack<object>();
                string type, id, name;
                Metadata meta;
                string line = ""; 
                line = sr.ReadLine();
                while(line != null)
                {
                    meta = RH.ReadMetadata(line);
                    if (meta.Type != "")
                    {
                        if(meta.Id != "") //is reference type
                        {
                            if(meta.IsRefId)
                            {
                                //register in previous object properties
                                var propertyForNewObjectInfo = new ClassLoadInfo()
                                {
                                    Id = meta.Id,
                                    Name = meta.Name,
                                };
                                objectsInfo[objectLoadOrder.Peek()].Add(propertyForNewObjectInfo);
                            }
                            else
                            {
                                if(objectLoadOrder.Count == 0) //no objects yet
                                {
                                    //Instantiate new, root class
                                    object newObject = GetInstance(meta.Type);
                                    objectDictionary.Add(meta.Id, newObject);
                                    objectsInfo.Add(newObject, new List<ClassLoadInfo>());
                                    objectLoadOrder.Push(newObject);
                                }
                                else
                                {
                                    object newObject = GetInstance(meta.Type);
                                    objectDictionary.Add(meta.Id, newObject);
                                    objectsInfo.Add(newObject, new List<ClassLoadInfo>());
                                    //register in previous object properties
                                    var propertyForNewObjectInfo = new ClassLoadInfo()
                                    {
                                        Id = meta.Id,
                                        Name= meta.Name,
                                    };
                                    objectsInfo[objectLoadOrder.Peek()].Add(propertyForNewObjectInfo);
                                    //
                                    objectLoadOrder.Push(newObject);
                                }
                            }
                        }
                        else //is value type
                        {
                            var objectToSetProperty = objectLoadOrder.Peek();
                            var property = objectToSetProperty.GetType().GetProperty(meta.Name);
                            object valueToSet = RH.LookForValue(line);
                            if (meta.Type == "System.Int32")
                                valueToSet = Convert.ToInt32(valueToSet);
                            property.SetValue(objectToSetProperty, valueToSet , null);
                        }
                    }
                    if (CheckForClassEnding(line)) // top object is finished
                    {
                        if (objectLoadOrder.Count != 0)
                            objectLoadOrder.Pop();
                    }
                    line = sr.ReadLine();
                }
                foreach (var item in objectDictionary.Values) // assign created objects referenced by REF_ID
                {
                    var propertiesList = objectsInfo[item];
                    foreach (var propertyInfo in propertiesList) //find object with that ID
                    {
                        var property = item.GetType().GetProperty(propertyInfo.Name);
                        property.SetValue(item,objectDictionary[propertyInfo.Id]);  //set property
                    }
                }
                return objectDictionary.Values.First();
            }  
        }



        bool CheckForClassEnding(string line)
        {
            if (line.Trim() == ")")
                return true;
            else
                return false;
        }
        bool CheckForCollectionEnding(string line)
        {
            if (line.Trim() == ">")
                return true;
            else
                return false;
        }

         object GetObject(string clazz, string[] prop, string[] value)
        {

            var obj = GetInstance(clazz);
            for (int i = 0; i < prop.Length; i++)
            {
                PropertyInfo proper = obj.GetType().GetProperty(prop[i]);
                proper.SetValue(obj, value[i], null);
            }

            return obj;
        }

         object GetInstance(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }
            return null;
        }

        /// <summary>
        /// wrapper for serialization process
        /// </summary>
        /// <param name="serializationStream"></param>
        /// <param name="graph"></param>
        public void Serialize(Stream serializationStream, object graph)
        {
            int recurenceLvl = -1;
            StreamWriter sw = new StreamWriter(serializationStream);
            if (IsReferenceType(graph.GetType()))
            {
                UnwrappedSerialization(sw, graph, null, recurenceLvl);
            }
            else
            {
                PrimitiveSerialization(sw, graph, recurenceLvl);
            }
            sw.Close();
        }

        private void UnwrappedSerialization(StreamWriter serializationWriter, object graph, PropertyInfo member, int recurenceLvl)
        {
            recurenceLvl++;
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            var type = graph.GetType();
       
            if (IsReferenceType(type))
            {
                ReferenceSerialization(serializationWriter, graph, member, recurenceLvl);
            }
            else
            {
                PrimitiveSerialization(serializationWriter, graph, recurenceLvl);
            }
        }

        private void PrimitiveSerialization(StreamWriter serializationWriter, object graph,  int recurenceLvl)
        {
            if (!( graph is null))
                serializationWriter.WriteLine("{2}{{Type={0} }}={1}", graph.GetType().FullName, graph.ToString(), Indent(recurenceLvl));
        }

        private void ReferenceSerialization(StreamWriter serializationWriter, object graph, PropertyInfo member, int recurenceLvl)
        {
            var id = idGenerator.GetId(graph, out bool isFirstTime);

            if (graph is IEnumerable)
            {
                WriteCollectionHeader(serializationWriter, graph, member, id, isFirstTime, recurenceLvl);
                var col = graph as IEnumerable;
                foreach (var item in col)
                {
                    UnwrappedSerialization(serializationWriter, item, member, recurenceLvl);
                }
                serializationWriter.WriteLine("{0}>", Indent(recurenceLvl));
            }
            else
            {
                WriteHeader(serializationWriter, graph, member, id, isFirstTime, recurenceLvl);
                if (isFirstTime)
                {
                    var props = graph.GetType().GetProperties();
                    foreach (var pro in props)
                    {
                        if (IsReferenceType(pro.PropertyType))
                        {
                            var obj = pro.GetValue(graph);
                            this.UnwrappedSerialization(serializationWriter, obj, pro, recurenceLvl);
                        }
                        else
                        {
                            WritePrimitiveHeader(serializationWriter, graph, pro, recurenceLvl + 1);
                        }
                    }
                }
                serializationWriter.WriteLine("{0})", Indent(recurenceLvl));
            }
        }

        bool IsReferenceType(Type type)
        {
            return (!type.IsPrimitive && !type.IsValueType && !(type == typeof(string)));
        }

        string Indent(int recurenceLvl)
        {
            return String.Concat(Enumerable.Repeat(indenter, recurenceLvl));
        }

        void WritePrimitiveHeader(StreamWriter serializationWriter, object graph, PropertyInfo member, int recurenceLvl)
        {
            serializationWriter.WriteLine("{3}{{Type={0} Name={1}}}={2}", member.GetValue(graph)?.GetType().FullName, member?.Name, member?.GetValue(graph), Indent(recurenceLvl));
        }

        void WriteHeader(StreamWriter serializationWriter, object graph, PropertyInfo member, long id, bool isFirstTime, int recurenceLvl)
        {
            if (isFirstTime)
            {
                serializationWriter.WriteLine("{3}{{Type={0} ID={1} Name={2}}} (", graph.GetType().FullName, id, member?.Name, Indent(recurenceLvl));
            }
            else
            {
                serializationWriter.WriteLine("{3}{{Type={0}  REF_ID={1} Name={2}}} (", graph.GetType().FullName, id, member?.Name, Indent(recurenceLvl));
            }
        }

        void WriteCollectionHeader(StreamWriter serializationWriter, object graph, PropertyInfo member, long id, bool isFirstTime, int recurenceLvl)
        {
            if (isFirstTime)
            {
                serializationWriter.WriteLine("{2}{{Type={0} ID={1} Name={2}}} <", graph.GetType().FullName, id, member?.Name, Indent(recurenceLvl));
            }
            else
            {
                serializationWriter.WriteLine("{2}{{Type={0}  REF_ID={1} Name={2}}} <", graph.GetType().FullName, id, member?.Name, Indent(recurenceLvl));
            }
        }

        public ISurrogateSelector SurrogateSelector
        {
            get { return surrogateSelector; }
            set { surrogateSelector = value; }
        }
        public SerializationBinder Binder
        {
            get { return binder; }
            set { binder = value; }
        }
        public StreamingContext Context
        {
            get { return context; }
            set { context = value; }
        }
    }
}