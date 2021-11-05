using System;

namespace MQSharp
{
    public class NoResponseFromBroker : Exception
    {
        public NoResponseFromBroker(string message)
            : base(message)
        {
        }

        public NoResponseFromBroker(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
