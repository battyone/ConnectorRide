using System;

namespace Knapcode.ConnectorRide.Core
{
    public class ConnectorClientException : Exception
    {
        public ConnectorClientException(string message) : base(message)
        {
        }
    }
}