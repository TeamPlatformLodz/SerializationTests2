using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace cokolwiek
{   
    [Serializable]
    public class Car : ISerializable
    {
        public string Model { get; set; } = "Model";
        public int Year { get; set; } = 1973;
        public Owner Owner { get; set; }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("car_model", Model, Model.GetType());
            info.AddValue("car_owner", Owner, Owner.GetType());
        }
    }

    [Serializable]
    public class Owner : ISerializable
    {
        public string Name { get; set; } = "Roman";
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("owner_name", Name, Name.GetType());
        }
    }
}
