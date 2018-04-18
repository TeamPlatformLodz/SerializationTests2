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

        public string Id { get => id;}
        public string Name { get; set; }

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
