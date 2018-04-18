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

using static cokolwiek.RH;
using static cokolwiek.TypeHandler;
using static cokolwiek.GenericCollectionCreator;

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
            idGenerator = new ObjectIDGenerator();
        }

        #region deserialize

        public object Deserialize(System.IO.Stream serializationStream)
        {
            using (StreamReader sr = new StreamReader(serializationStream))
            {
                // stores information to what objects given object has references
                Dictionary<object, List<ClassLoadInfo>> objectReferences = new Dictionary<object, List<ClassLoadInfo>>();
                Dictionary<string, object> objectDictionary = new Dictionary<string, object>();
                Stack<object> orderStack = new Stack<object>();
                Dictionary<string, List<object>> placeholderLists = new Dictionary<string, List<object>>();
                Dictionary<object, string> placeholderRealTypes = new Dictionary<object, string>();
                Dictionary<object, string> placeholderElementTypes = new Dictionary<object, string>();

                Metadata meta;
                string line = ""; 
                line = sr.ReadLine();
                #region read lines
                while (line != null)
                {
                    meta = ReadMetadata(line);
                    if (meta.Type != "")
                    {
                        if(meta.Id != "") //is reference type
                        {
                            if(meta.IsRefId)
                            {
                                RegisterRefIdObject(orderStack, objectReferences, meta);                                            
                            }
                            else // not a refID, so construct new object
                            {
                                if(orderStack.Count == 0) //object is a top object
                                {
                                    RegisterTopLevelObject(objectDictionary,objectReferences, orderStack, meta);
                                }
                                else    //object is other object property
                                {
                                    RegisterSubObject(line, objectDictionary, objectReferences, orderStack, meta, placeholderLists, placeholderRealTypes, placeholderElementTypes);
                                }
                            }
                        }
                        else //is value type
                        {
                            RegisterValueObject(orderStack, line, meta);
                        }
                    }
                    if (CheckForClassEnding(line) || CheckForCollectionEnding(line)) //move up one element
                    {
                        if (orderStack.Count != 0)
                            orderStack.Pop();
                    }
                    line = sr.ReadLine();
                }
                #endregion

                ReplacePlaceHolderLists();

                SetReferences(objectDictionary, objectReferences);

                return objectDictionary.Values.First(); // return first found object

                #region local methods
                void ReplacePlaceHolderLists()
                {
                    foreach (var key in placeholderLists.Keys)
                    {
                        var list = placeholderLists[key];
                        var typeName = placeholderRealTypes[list];
                        string elementType = placeholderElementTypes[list];
                        var collection = CreateProperCollection(typeName, elementType);
                        if (collection is IList)
                        {
                            var listCollection = collection as IList;
                            var items = objectReferences[list];
                            foreach (var item in items)
                            {
                                var itemToLoad = objectDictionary[item.Id];
                                listCollection.Add(itemToLoad);
                            }
                            objectDictionary[key] = listCollection; //swap proper list with placeholder
                            var tempInfo = objectReferences[list];
                            objectReferences.Remove(list);
                            objectReferences.Add(listCollection, tempInfo);
                        }
                        #region
                        //if (collection is List<String>)
                        //{
                        //    var listCollection = collection as List<String>;
                        //    var keys = objectReferences[list];
                        //    foreach (var productKey in keys)
                        //    {
                        //        var itemToLoad = objectDictionary[productKey.Id];
                        //        listCollection.Add((String)itemToLoad);
                        //    }
                        //    objectDictionary[key] = listCollection; //swap proper list with placeholder
                        //    var tempInfo = objectReferences[list];
                        //    objectReferences.Remove(list);
                        //    objectReferences.Add(listCollection, tempInfo);
                        //}
                        //if (collection is List<Product>)
                        //{
                        //    var listCollection = collection as List<Product>;
                        //    var prodcucts = objectReferences[list];
                        //    foreach (var product in prodcucts)
                        //    {
                        //        var itemToLoad = objectDictionary[product.Id];
                        //        listCollection.Add((Product)itemToLoad);
                        //    }
                        //    objectDictionary[key] = listCollection; //swap proper list with placeholder
                        //    var tempInfo = objectReferences[list];
                        //    objectReferences.Remove(list);
                        //    objectReferences.Add(listCollection, tempInfo);
                        //}
                        //if (collection is List<Invoice>)
                        //{
                        //    var listCollection = collection as List<Invoice>;
                        //    var invoices = objectReferences[list];
                        //    foreach (var invoice in invoices)
                        //    {
                        //        var itemToLoad = objectDictionary[invoice.Id];
                        //        listCollection.Add((Invoice)itemToLoad);
                        //    }
                        //    objectDictionary[key] = listCollection;
                        //    var tempInfo = objectReferences[list];
                        //    objectReferences.Remove(list);
                        //    objectReferences.Add(listCollection, tempInfo);
                        //}
                        //if (collection is List<ProductState>)
                        //{
                        //    var listCollection = collection as List<ProductState>;
                        //    var pStates = objectReferences[list];
                        //    foreach (var state in pStates)
                        //    {
                        //        var itemToLoad = objectDictionary[state.Id];
                        //        listCollection.Add((ProductState)itemToLoad);
                        //    }
                        //    objectDictionary[key] = listCollection;
                        //    var tempInfo = objectReferences[list];
                        //    objectReferences.Remove(list);
                        //    objectReferences.Add(listCollection, tempInfo);
                        //}
                        #endregion
                    }
                }
                #endregion
            }  
        }
        private void RegisterTopLevelObject(Dictionary<string, object> objectDictionary, Dictionary<object, List<ClassLoadInfo>> objectReferences, Stack<object> order, Metadata metadata)
        {
            //Instantiate new, root class
            object newObject = GetInstance(metadata.Type);
            objectDictionary.Add(metadata.Id, newObject);
            objectReferences.Add(newObject, new List<ClassLoadInfo>());
            order.Push(newObject);
        }

        private void RegisterSubObject(string line, Dictionary<string, object> objectDictionary, Dictionary<object, List<ClassLoadInfo>> objectReferences, Stack<object> order, Metadata metadata,
            Dictionary<string, List<object>> placeholderLists, Dictionary<object, string> placeholderRealTypes, Dictionary<object, string> placeholderElementTypes)
        {
            object newObject = "BadValue"; //dummy value                     
            if (RH.ContainsCollection(line))  //is collection ?
            {
                newObject = new List<object>();
                placeholderLists.Add(metadata.Id, (List<object>)newObject); //register new placeholder
                placeholderRealTypes.Add(newObject, metadata.Type);
                string firstTypeName = RH.LookForListElementType(line);
                placeholderElementTypes.Add(newObject, firstTypeName);
            }
            else
            {
                newObject = GetInstance(metadata.Type);
            }

            objectDictionary.Add(metadata.Id, newObject); //register object
            objectReferences.Add(newObject, new List<ClassLoadInfo>());//register missing property

            //register in previous object properties
            var propertyForNewObjectInfo = new ClassLoadInfo()
            {
                Id = metadata.Id,
                Name = metadata.Name,
            };
            objectReferences[order.Peek()].Add(propertyForNewObjectInfo);
            //
            order.Push(newObject);
        }

        private void RegisterRefIdObject(Stack<object> order, Dictionary<object, List<ClassLoadInfo>> objectReferences, Metadata metadata)
        {
            //register in previous object properties
            var propertyForNewObjectInfo = new ClassLoadInfo()
            {
                Id = metadata.Id,
                Name = metadata.Name,
            };
            var topObject = order.Peek();
            if (topObject is IList)
            {
                var list = topObject as IList;
                list.Add(propertyForNewObjectInfo);
            }
            else
            {
                objectReferences[topObject].Add(propertyForNewObjectInfo);
            }
            order.Push(new object());
        }

        private void RegisterValueObject(Stack<object> order, string line, Metadata metadata)
        {
            var valueToSet = GetPrimitiveValueToSet(line, metadata.Type);
            var objectToSet = order.Peek();
            if (objectToSet is IList) //add to collection
            {
                var placeList = objectToSet as IList;
                placeList.Add(valueToSet);
            }
            else //set property
            {
                var property = objectToSet.GetType().GetProperty(metadata.Name);
                property.SetValue(objectToSet, valueToSet, null);
            }
        }

        private void SetReferences(Dictionary<string, object> objectDictionary, Dictionary<object, List<ClassLoadInfo>> objectReferences)
        {
            foreach (var item in objectDictionary.Values) // assign created objects referenced by REF_ID
            {
                if (!IsPrimitive(item.GetType().ToString())) // for reference types assign their reference fields
                {
                    if (!(item is ICollection)) // list has elements already added
                    {
                        var propertiesList = objectReferences[item];
                        foreach (var propertyInfo in propertiesList) //find object with that ID
                        {
                            var property = item.GetType().GetProperty(propertyInfo.Name); 
                            var valueToSet = objectDictionary[propertyInfo.Id];
                            property.SetValue(item, valueToSet);  //set property
                        }
                    }
                }
            }
        }

        private object GetPrimitiveValueToSet(string line, string type)
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

        object CreateProperCollection(string typeName, string elementTypeName)
        {
            var firstTypeName = elementTypeName;
            Type typeOfFirstElement;
            if (IsPrimitive(firstTypeName))
            {
                typeOfFirstElement = Type.GetType(firstTypeName);
            }
            else
            {
                typeOfFirstElement = GetInstance(firstTypeName).GetType();
            }
            var result = TryGetList();
            if(result != null)
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

        #endregion

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

        string Indent(int recurenceLvl)
        {
            return String.Concat(Enumerable.Repeat(indenter, recurenceLvl));
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