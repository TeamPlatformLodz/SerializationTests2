using System;
using System.Runtime.Serialization;

namespace Shop
{ 
    [Serializable]
    public class NotEnoughProductException : Exception
    {
        public NotEnoughProductException(string message) : base(message)
        {
        }
    }
}