using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cokolwiek
{
    public static class TypeHandler
    {
        public static bool IsReferenceType(Type type)
        {
            return (!type.IsPrimitive && !type.IsValueType && !(type == typeof(string)));
        }

        public static bool IsPrimitive(string typeName)
        {
            if (typeName == "System.String")
            {
                return true;
            }
            if (typeName == "Shop.Percentage")
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

        public static object GetInstance(string strFullyQualifiedName)
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
    }
}
