using System;
using System.Runtime.Serialization;

namespace Shop.Exceptions
{
    [Serializable]
    internal class DeleteReferencesException : Exception
    {
        public DeleteReferencesException()
        {
        }

        public DeleteReferencesException(string message) : base(message)
        {
        }

        public DeleteReferencesException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeleteReferencesException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}