using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop
{
    [Serializable]
    public class Product
    {
        private string id;

        public string Id { get => id; set => id = value; }
        public string Name { get; set; }
        public string Model { get; set; } = "Yrok";

        public Product() { }

        public Product(string name)
        {
            id = Guid.NewGuid().ToString();
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
