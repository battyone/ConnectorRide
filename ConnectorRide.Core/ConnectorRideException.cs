using System;

namespace Knapcode.ConnectorRide.Core
{
    public class ConnectorRideException : Exception
    {
        public ConnectorRideException(string message) : base(message)
        {
        }
    }
}