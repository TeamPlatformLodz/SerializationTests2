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
                Dictionary<object, List<ClassLoadInfo>> objectsInfo = new Dictionary<object, List<ClassLoadInfo>>();
                Dictionary<string, object> objectDictionary = new Dictionary<string, object>();
                Stack<object> objectLoadOrder = new Stack<object>();
                Dictionary<string, List<object>> placeholderLists = new Dictionary<string, List<object>>();
                Dictionary<object, string> placeholderRealTypes = new Dictionary<object, string>();
                Dictionary<object, string[]> placeholderElementTypes = new Dictionary<object, string[]>();

                Metadata meta;
                string line = ""; 
                line = sr.ReadLine();
                #region read lines
                while (line != null)
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
                                objectLoadOrder.Push(new object());
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
                                        string firstTypeName = RH.LookForListElementType(line);
                                        string secondTypeName = RH.LookForSecondElementType(line);
                                        placeholderElementTypes.Add(newObject, new string[] { firstTypeName, secondTypeName });
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
                            var valueToSet = GetValueToSet(line, meta.Type);
                            var objectToSet = objectLoadOrder.Peek();
                            if(objectToSet is IList) //add to collection
                            {
                                var placeList =  objectToSet as IList;
                                placeList.Add(valueToSet);
                            }
                            else //set property
                            {
                                var property = objectToSet.GetType().GetProperty(meta.Name);
                                property.SetValue(objectToSet, valueToSet, null);
                            }
                        }
                    }
                    if (CheckForClassEnding(line)) // top object is finished
                    {
                        if (objectLoadOrder.Count != 0)
                            objectLoadOrder.Pop();
                    }
                    if (CheckForCollectionEnding(line) && objectLoadOrder.Peek() is IList)
                    {
                        if (objectLoadOrder.Count != 0)
                            objectLoadOrder.Pop();
                    }
                    line = sr.ReadLine();
                }
#endregion

                foreach (var key in placeholderLists.Keys)
                {
                    var list = placeholderLists[key];
                    var typeName = placeholderRealTypes[list];
                    string[] typeNames = placeholderElementTypes[list];
                    var collection = CreateProperCollection(typeName, typeNames);
                    if (collection is List<Client>)
                    {
                        var listCollection = collection as List<Client>;
                        var clients = objectsInfo[list];
                        foreach (var client in clients)
                        {
                            var itemToLoad = objectDictionary[client.Id];
                            listCollection.Add((Client)itemToLoad);
                        }
                        objectDictionary[key] = listCollection; //swap proper list with placeholder
                        var tempInfo = objectsInfo[list];
                        objectsInfo.Remove(list);
                        objectsInfo.Add(listCollection, tempInfo);
                    }
                    if (collection is List<String>)
                    {
                        var listCollection = collection as List<String>;
                        var keys = objectsInfo[list];
                        foreach (var productKey in keys)
                        {
                            var itemToLoad = objectDictionary[productKey.Id];
                            listCollection.Add((String)itemToLoad);
                        }
                        objectDictionary[key] = listCollection; //swap proper list with placeholder
                        var tempInfo = objectsInfo[list];
                        objectsInfo.Remove(list);
                        objectsInfo.Add(listCollection, tempInfo);
                    }
                    if (collection is List<Product>)
                    {
                        var listCollection = collection as List<Product>;
                        var prodcucts = objectsInfo[list];
                        foreach (var product in prodcucts)
                        {
                            var itemToLoad = objectDictionary[product.Id];
                            listCollection.Add((Product)itemToLoad);
                        }
                        objectDictionary[key] = listCollection; //swap proper list with placeholder
                        var tempInfo = objectsInfo[list];
                        objectsInfo.Remove(list);
                        objectsInfo.Add(listCollection, tempInfo);
                    }
                    if (collection is List<Invoice>)
                    {
                        var listCollection = collection as List<Invoice>;
                        var invoices = objectsInfo[list];
                        foreach (var invoice in invoices)
                        {
                            var itemToLoad = objectDictionary[invoice.Id];
                            listCollection.Add((Invoice)itemToLoad);
                        }
                        objectDictionary[key] = listCollection;
                        var tempInfo = objectsInfo[list];
                        objectsInfo.Remove(list);
                        objectsInfo.Add(listCollection, tempInfo);
                    }
                    if (collection is List<ProductState>)
                    {
                        var listCollection = collection as List<ProductState>;
                        var pStates = objectsInfo[list];
                        foreach (var state in pStates)
                        {
                            var itemToLoad = objectDictionary[state.Id];
                            listCollection.Add((ProductState)itemToLoad);
                        }
                        objectDictionary[key] = listCollection;
                        var tempInfo = objectsInfo[list];
                        objectsInfo.Remove(list);
                        objectsInfo.Add(listCollection, tempInfo);
                    }
  
                }

                foreach (var item in objectDictionary.Values) // assign created objects referenced by REF_ID
                {
                    if (! (item is Percentage))
                    {
                        var propertiesList = objectsInfo[item];
                        if (item is ICollection)
                        {
                            //var list = item as IList;
                            //foreach (var propertyInfo in propertiesList) //find object with that ID
                            //{
                            //    list.Add(objectDictionary[propertyInfo.Id]);  //add element
                            //}
                        }
                        else
                        {
                            foreach (var propertyInfo in propertiesList) //find object with that ID
                            {
                                var property = item.GetType().GetProperty(propertyInfo.Name);
                                var valueToSet = objectDictionary[propertyInfo.Id];
                                property.SetValue(item, valueToSet);  //set property
                            }
                        }
                    }
                }
                return objectDictionary.Values.First();
            }  
        }

        private object GetValueToSet(string line, string type)
        {
            object valueToSet = RH.LookForValue(line);
            if (type == "System.Int32")
                valueToSet = Convert.ToInt32(valueToSet);
            if (type == "System.Double")
                valueToSet = Convert.ToDouble(valueToSet);
            if (type == "System.Single")
                valueToSet = Convert.ToSingle(valueToSet);
            if (type == "System.Decimal")
                valueToSet = Convert.ToDecimal(valueToSet);
            if (type == "System.DateTime")
                valueToSet = Convert.ToDateTime(valueToSet);
            return valueToSet;
        }

        object CreateProperCollection(string typeName, string[] elementTypeName)
        {
            var firstTypeName = elementTypeName[0];
            Type typeOfFirstElement;
            if (IsPrimitive(firstTypeName))
            {
                typeOfFirstElement = Type.GetType(elementTypeName[0]);
            }
            else
            {
                typeOfFirstElement = GetInstance(elementTypeName[0]).GetType();
            }
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
            result = TryGetDictionary();
            if (result != null)
            {
                return result;
            }
            return null;

            object TryGetList()
            {
                if (typeName.Contains("System.Collections.Generic.List"))
                {
                    return CreateGenericList(typeOfFirstElement);
                }
                return null;
            }
            object TryGetObservableCollection( )
            {
                if (typeName.Contains("System.Collections.Generic.ObservableCollection"))
                {
                    return CreateGenericObservableCollection(typeOfFirstElement);
                }
                return null;
            }
            object TryGetDictionary()
            {
                if (typeName.Contains("System.Collections.Generic.Dictionary"))
                {
                    Type typeOfSecondElement;
                    if (IsPrimitive(elementTypeName[1]))
                    {
                        typeOfSecondElement = Type.GetType(elementTypeName[1]);
                    }
                    else
                    {
                        typeOfSecondElement = GetInstance(elementTypeName[1]).GetType();
                    }
                    return CreateGenericDictionary(typeOfFirstElement, typeOfSecondElement); //two type params
                }
                return null;
            }
        }

        object GetPrimitiveInstance(string typeName)
        {
            if (typeName == "System.Int32")
            {
                int res = 0;
                return res;
            }
            if (typeName == "System.Double")

            {
                double res = 0;
                return res;
            }
            if (typeName == "System.Single")
            {
                float res = 0;
                return res;
            }
            if (typeName == "System.Decimal")
            {
                decimal res = 0;
                return res;
            }
            if (typeName == "System.DateTime")
            {
                DateTime res = DateTime.MinValue;
                return res;
            }
            return null;
        }

        bool IsPrimitive(string typeName)
        {
            if (typeName == "System.String")
            {
                return true;
            }
            if (typeName == "System.String[]")
            {
                return true;
            }
            if (typeName == "System.Int32")
            {
                return true;
            }
            if (typeName == "System.Int16")
            {
                return true;
            }
            if (typeName == "System.Double")
            {
                return true;
            }
            if (typeName == "System.Single")
            {
                return true;
            }
            if (typeName == "System.Decimal")
            {
                return true;
            }
            if (typeName == "System.DateTime")
            {
                return true;
            }
            return false;
        }

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
        object CreateGenericDictionary(Type firstElementType, Type secondElementType)
        {
            Type[] typeArgs = new Type[] { firstElementType, secondElementType };
            var makeme = typeof(Dictionary<,>).MakeGenericType(typeArgs);
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