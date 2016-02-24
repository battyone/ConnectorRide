using System;

namespace Knapcode.ConnectorRide.Core
{
    public class ClientException : Exception
    {
        public ClientException(string message) : base(message)
        {
        }
    }
}