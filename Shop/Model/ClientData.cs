using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Model
{
    public class ClientData
    {
        private string lastName;
        private string firstName;

        public string FirstName { get => firstName; set { firstName = value; IsFirstNameChanged = true; } }
        public bool IsFirstNameChanged { get; set; } = false;
        public string LastName { get => lastName; set { lastName = value; IsLastNameChanged = true; } }
        public bool IsLastNameChanged { get; set; } = false;
    }
}
