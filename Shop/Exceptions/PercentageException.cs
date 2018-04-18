using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Exceptions
{
    [Serializable]
    public class PercentageException : ArgumentException
    {
        public PercentageException(string message) : base(message)
        {
        }
    }
}
