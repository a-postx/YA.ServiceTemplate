using System;

namespace YA.ServiceTemplate.Application.Exceptions
{
    public class CorrelationIdNotFoundException : Exception
    {
        public CorrelationIdNotFoundException()
        {
        }

        public CorrelationIdNotFoundException(string message) : base(message)
        {
        }

        public CorrelationIdNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
