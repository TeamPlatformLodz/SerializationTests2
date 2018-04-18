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
using System.Collections.ObjectModel;
using Shop;

namespace cokolwiek
{
    class CustomFormatter : IFormatter
    {
        SerializationBinder binder;
        StreamingContext context;
        ISurrogateSelector surrogateSelector;
        ObjectIDGenerator idGenerator;
        //Assembly usedAssembly;

        private readonly string indenter = "    ";

        public CustomFormatter()
        {
            context = new StreamingContext(StreamingContextStates.All);
            idGenerator = new ObjectIDGenerator();
            //usedAssembly = Assembly.LoadWithPartialName("Shop");
        }

        public object Deserialize(System.IO.Stream serializationStream)
        {
            using (StreamReader sr = new StreamReader(serializationStream))
            {
                Dictionary<object, List<ClassLoadInfo>> objectsInfo = new Dictionary<object, List<ClassLoadInfo>>();
                Dictionary<string, object> objectDictionary = new Dictionary<string, object>();
                Stack<object> objectLoadOrder = new Stack<object>();
                Dictionary<string, List<object>> placeholderLists = new Dictionary<string, List<object>>();
                Dictionary<List<object>, string> placeholderListsId = new Dictionary<List<object>, string>;
                Dictionary<object, string> placeholderRealTypes = new Dictionary<object, string>();
                Dictionary<object, string> placeholderElementTypes = new Dictionary<object, string>();

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
                                var topObject = objectLoadOrder.Peek();
                                if (topObject is IList)
                                {
                                    var list = topObject as IList;
                                    list.Add(propertyForNewObjectInfo);
                                }
                                else
                                {
                                    objectsInfo[topObject].Add(propertyForNewObjectInfo);
                                }
                            }
                            else // not a refID, so construct new object
                            {
                                if(objectLoadOrder.Count == 0) //no objects yet
                                {
                                    //Instantiate new, root class
                                    object newObject = GetInstance(meta.Type);
                                    objectDictionary.Add(meta.Id, newObject);
                                    objectsInfo.Add(newObject, new List<ClassLoadInfo>());
                                    objectLoadOrder.Push(newObject);
                                }
                                else    //not a top object
                                {
                                    object newObject = "BadValue"; //dummy value                     
                                    if (RH.ContainsCollection(line))  //is collection ?
                                    {
                                        newObject = new List<object>();
                                        placeholderLists.Add(meta.Id, (List<object>)newObject); //register new placeholder
                                        placeholderRealTypes.Add(newObject, meta.Type);
                                        placeholderElementTypes.Add(newObject, RH.LookForListElementType(line));
                                    }
                                    else
                                    {
                                        newObject = GetInstance(meta.Type);
                                    }

                                    objectDictionary.Add(meta.Id, newObject); //register object
                                    objectsInfo.Add(newObject, new List<ClassLoadInfo>());//register missing property
                                    //register in previous object properties
                                    var propertyForNewObjectInfo = new ClassLoadInfo()
                                    {
                                        Id = meta.Id,
                                        Name = meta.Name,
                                    };
                                    objectsInfo[objectLoadOrder.Peek()].Add(propertyForNewObjectInfo);
                                    //
                                    objectLoadOrder.Push(newObject);
                                }
                            }
                        }
                        else //is value type
                        {
                            object valueToSet = RH.LookForValue(line);
                            if (meta.Type == "System.Int32")
                                valueToSet = Convert.ToInt32(valueToSet);
                            if (meta.Type == "System.Double")
                                valueToSet = Convert.ToDouble(valueToSet);
                            if (meta.Type == "System.Single")
                                valueToSet = Convert.ToSingle(valueToSet);
                            if (meta.Type == "System.Decimal")
                                valueToSet = Convert.ToDecimal(valueToSet);
                            if (meta.Type == "System.DateTime")
                                valueToSet = Convert.ToDateTime(valueToSet);
                            var objectToSet = objectLoadOrder.Peek();
                            if(objectToSet is ICollection)
                            {
                                var placeList =  objectToSet as IList;
                                placeList.Add(valueToSet);
                            }
                            else
                            {
                                var property = objectToSet.GetType().GetProperty(meta.Name);
                                property.SetValue(objectToSet, valueToSet, null);
                            }
                        }
                    }
                    if (CheckForClassEnding(line) ) // top object is finished
                    {
                        if (objectLoadOrder.Count != 0)
                            objectLoadOrder.Pop();
                    }

                    line = sr.ReadLine();
                }
                foreach (var key in placeholderLists.Keys)
                {
                    var list = placeholderLists[key]
                }

                foreach (var item in objectDictionary.Values) // look for placehoolder lists and rebuild
                {
                    if (item is IList) // item is collection, then build proper collection type and rewrite values
                    {
                        var typeName = placeholderRealTypes[item];
                        var elemTypeName = placeholderElementTypes[item];
                        var collection = CreateProperCollection(typeName, elemTypeName);
                        
                    }
                }
                foreach (var item in objectDictionary.Values) // assign created objects referenced by REF_ID
                {
                    var propertiesList = objectsInfo[item];
                    if (item is IList)
                    {
                        var list = item as IList;
                        foreach (var propertyInfo in propertiesList) //find object with that ID
                        {
                            list.Add(objectDictionary[propertyInfo.Id]);  //add element
                        }
                    }
                    else
                    {
                        foreach (var propertyInfo in propertiesList) //find object with that ID
                        {
                            var property = item.GetType().GetProperty(propertyInfo.Name);
                            property.SetValue(item, objectDictionary[propertyInfo.Id]);  //set property
                        }
                    }
                }
                return objectDictionary.Values.First();
            }  
        }
        object CreateProperCollection(string typeName, string elementTypeName)
        {
            var typeOfElement = GetInstance(elementTypeName).GetType();
            var result = TryGetList();
            if(result != null)
            {
                return result;
            }
            result = TryGetObservableCollection();
            if (result != null)
            {
                return result;
            }
            //result = TryGetDictionary();
            if (result != null)
            {
                return result;
            }
            return null;

            object TryGetList()
            {
                if (typeName.Contains("System.Collections.Generic.List"))
                {
                    return CreateGenericList(typeOfElement);
                }
                return null;
            }
            object TryGetObservableCollection( )
            {
                if (typeName.Contains("System.Collections.Generic.ObservableCollection"))
                {
                    return CreateGenericObservableCollection(typeOfElement);
                }
                return null;
            }
        }

        #region old
        //bool IfCollectionAdd(Stack<object> objectOrder, object toAdd)
        //{
        //    var topObject = objectOrder.Peek();

        //    if (topObject is IList) //add to collection instead of setting properties
        //    {
        //        var prevCollection = objectOrder.Peek() as IList;
        //        prevCollection.Add(toAdd);
        //        return true;
        //    }
        //    if (topObject is ObservableCollection<Invoice>) //checkl for observable coll
        //    {
        //        var collection = topObject as ObservableCollection<Invoice>;
        //        collection.Add(toAdd as Invoice);
        //        return true;
        //    }
        //    if (topObject is ObservableCollection<ProductState>) //checkl for observable coll
        //    {
        //        var collection = topObject as ObservableCollection<ProductState>;
        //        collection.Add(toAdd as ProductState);
        //        return true;
        //    }
        //    return false;
        //}
        #endregion

        object CheckForCollectionType(string line, Metadata metadata)
        {
            if (RH.ContainsCollection(line))  //is collection ?
            {
                string typeName = RH.LookForListElementType(line);
                var colElemType = Type.GetType(typeName);
                if (colElemType != null) // there is an generic parameter
                {
                    var collection = TryGetList(colElemType);
                    if (collection != null)
                    {
                        return collection;
                    }
                    collection = TryGetObservableCollection(colElemType);
                    if (collection != null)
                    {
                        return collection;
                    }
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
            //helper local methods
            object TryGetList(Type typeOfElement)
            {
                if (metadata.Type.Contains("System.Collections.Generic.List"))
                {
                    return CreateGenericList(typeOfElement);
                }
                return null;
            }
            object TryGetObservableCollection(Type typeOfElement)
            {
                if (metadata.Type.Contains("System.Collections.Generic.ObservableCollection"))
                {
                    return CreateGenericObservableCollection(typeOfElement);
                }
                return null;
            }  
            
        }

        object CreateGenericList(Type typeOfElement)
        {
            Type[] typeArgs = new Type[] { typeOfElement };
            var makeme = typeof(List<>).MakeGenericType(typeArgs);
            object o = Activator.CreateInstance(makeme);
            return o;
        }
        object CreateGenericObservableCollection(Type typeOfElement)
        {
            Type[] typeArgs = new Type[] { typeOfElement };
            var makeme = typeof(ObservableCollection<>).MakeGenericType(typeArgs);
            object o = Activator.CreateInstance(makeme);
            return o;
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

        #region serialization
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
                serializationWriter.WriteLine("{3}{{Type={0} ID={1} Name={2}}} <", graph.GetType().FullName, id, member?.Name, Indent(recurenceLvl));
            }
            else
            {
                serializationWriter.WriteLine("{3}{{Type={0}  REF_ID={1} Name={2}}} <", graph.GetType().FullName, id, member?.Name, Indent(recurenceLvl));
            }
        }
        #endregion
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