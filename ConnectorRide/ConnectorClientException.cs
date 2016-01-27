using System;

namespace Knapcode.ConnectorRide
{
    public class ConnectorClientException : Exception
    {
        public ConnectorClientException(string message) : base(message)
        {
        }
    }
}