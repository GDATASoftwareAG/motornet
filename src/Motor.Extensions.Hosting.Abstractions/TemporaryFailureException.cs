using System;

namespace Motor.Extensions.Hosting.Abstractions
{
    public class TemporaryFailureException : Exception
    {
        public TemporaryFailureException()
        {
            
        }

        public TemporaryFailureException(string message) : base(message)
        {
            
        }

        public TemporaryFailureException(string message, Exception ex) : base(message, ex)
        {
            
        }
    }
}