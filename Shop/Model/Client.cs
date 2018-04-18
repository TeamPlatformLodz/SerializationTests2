using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Shop
{
    [Serializable]
    public class Client : ISerializable
    {
        private string id;

        public string Id { get => id; set => id = value; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public Client() { }

        public Client (string firstName, string lastName)
        {
            id = Guid.NewGuid().ToString();
            FirstName = firstName;
            LastName = lastName;
        }

        public override string ToString()
        {
            return FirstName + " " + LastName;
        }

        protected Client(SerializationInfo info, StreamingContext context)
        {
            id = info.GetString("Client_id");
            FirstName = info.GetString("Client_fn");
            LastName = info.GetString("Client_ln");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Client_id", id);
            info.AddValue("Client_fn", FirstName);
            info.AddValue("Client_ln", LastName);
        }
    }
}
