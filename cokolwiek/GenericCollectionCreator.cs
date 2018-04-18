using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cokolwiek
{
    public static class GenericCollectionCreator
    {
        public static object CreateGenericList(Type typeOfElement)
        {
            Type[] typeArgs = new Type[] { typeOfElement };
            var makeme = typeof(List<>).MakeGenericType(typeArgs);
            object o = Activator.CreateInstance(makeme);
            return o;
        }
        public static object CreateGenericObservableCollection(Type typeOfElement)
        {
            Type[] typeArgs = new Type[] { typeOfElement };
            var makeme = typeof(ObservableCollection<>).MakeGenericType(typeArgs);
            object o = Activator.CreateInstance(makeme);
            return o;
        }
        public static object CreateGenericDictionary(Type firstElementType, Type secondElementType)
        {
            Type[] typeArgs = new Type[] { firstElementType, secondElementType };
            var makeme = typeof(Dictionary<,>).MakeGenericType(typeArgs);
            object o = Activator.CreateInstance(makeme);
            return o;
        }
    }
}
