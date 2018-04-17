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

        private readonly string indenter = "    ";

        public CustomFormatter()
        {
            context = new StreamingContext(StreamingContextStates.All);
        }

        public object Deserialize(System.IO.Stream serializationStream)
        {
            StreamReader sr = new StreamReader(serializationStream);

            // Get Type from serialized data.
            string line = sr.ReadLine();
            char[] delim = new char[] { '=' };
            string[] sarr = line.Split(delim);
            string className = sarr[1];
            Type t = Type.GetType(className);

            // Create object of just found type name.
            Object obj = FormatterServices.GetUninitializedObject(t);

            // Get type members.
            MemberInfo[] members = FormatterServices.GetSerializableMembers(obj.GetType(), Context);

            // Create data array for each member.
            object[] data = new object[members.Length];

            // Store serialized variable name -> value pairs.
            StringDictionary sdict = new StringDictionary();
            while (sr.Peek() >= 0)
            {
                line = sr.ReadLine();
                sarr = line.Split(delim);

                // key = variable name, value = variable value.
                sdict[sarr[0].Trim()] = sarr[1].Trim();
            }
            sr.Close();

            // Store for each member its value, converted from string to its type.
            for (int i = 0; i < members.Length; ++i)
            {
                FieldInfo fi = ((FieldInfo)members[i]);
                if (!sdict.ContainsKey(fi.Name))
                    throw new SerializationException("Missing field value : " + fi.Name);
                data[i] = System.Convert.ChangeType(sdict[fi.Name], fi.FieldType);
            }

            // Populate object members with theri values and return object.
            return FormatterServices.PopulateObjectMembers(obj, members, data);
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
            ObjectIDGenerator idGenerator = new ObjectIDGenerator();
            UnwrappedSerialization(sw, graph, ref idGenerator, recurenceLvl);
            sw.Close();
        }

        public void UnwrappedSerialization(StreamWriter serializationWriter, object graph, ref ObjectIDGenerator idGen, int recurenceLvl)
        {
            recurenceLvl++;
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            var type = graph.GetType();
       
            if (IsReferenceType(type))
            {
                ReferenceSerialization(serializationWriter, graph, ref idGen, recurenceLvl);
            }
            else if (type == typeof(IEnumerable))
            {
                var current = ((IEnumerable) graph).GetEnumerator().Current;
                if (current != null)
                {
                    foreach (var elem in (IEnumerable)graph)
                    {
                        UnwrappedSerialization(serializationWriter, elem, ref idGen, recurenceLvl);
                    }
                }
            }
            else
            {
                PrimitiveSerialization(serializationWriter, graph, ref idGen, recurenceLvl);
            }
        }



        private void PrimitiveSerialization(StreamWriter serializationWriter, object graph, ref ObjectIDGenerator idGen, int recurenceLvl)
        {

            WriteHeader(serializationWriter, graph, ref idGen, recurenceLvl);

        }
        private void ReferenceSerialization(StreamWriter serializationWriter, object graph, ref ObjectIDGenerator idGen, int recurenceLvl)
        {

            WriteHeader(serializationWriter, graph, ref idGen, recurenceLvl);
            var props = graph.GetType().GetProperties();
            foreach (var pro in props)
            {
                if (IsReferenceType(pro.PropertyType))
                {

                    //                    if (pro.PropertyType == typeof(IEnumerable))
                    //                    {
                    //                        var collection = (IEnumerable) pro.GetValue(graph);
                    //
                    //                        foreach (var element in collection)
                    //                        {
                    //                            
                    //                        }
                    //                    }
                    //                    else
                    {
                        var obj = pro.GetValue(graph);
                        this.UnwrappedSerialization(serializationWriter, obj, ref idGen, recurenceLvl-1);
                    }
                }
            }

        }

        bool IsReferenceType(Type pro)
        {
            return !pro.IsPrimitive && !(pro == typeof(string));
        }

        string Indent(int recurenceLvl)
        {
            return String.Concat(Enumerable.Repeat(indenter, recurenceLvl));
        }

        void WriteHeader(StreamWriter serializationWriter, object graph, ref ObjectIDGenerator idGen, int recurenceLvl)
        {
            // Get fields that are to be serialized.
            MemberInfo[] members = FormatterServices.GetSerializableMembers(graph.GetType(), Context);

            // Get fields data.
            object[] subobjects = FormatterServices.GetObjectData(graph, members);

            // Write class name and all fields & values to file   
            var id = idGen.GetId(graph, out _);
            serializationWriter.WriteLine("{2}@ID={1} ClassName={0}", graph.GetType().FullName, id, Indent(recurenceLvl));
            {
                for (int i = 0; i < subobjects.Length; ++i)
                {
                    id = idGen.GetId(subobjects[i], out _);
                    serializationWriter.WriteLine("{3}@ID={2} {0}={1}", members[i].Name, subobjects[i].ToString(), id, Indent(recurenceLvl + 1));
                }
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