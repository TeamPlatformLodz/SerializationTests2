using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Model
{
    public class ProductData
    {
        private string name;
        public string Name { get => name; set { name = value; IsNameChanged = true; } }
        public bool IsNameChanged { get; set; } = false;
    }
}
