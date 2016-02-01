using System;

namespace Knapcode.ConnectorRide.Web.Services
{
    public class ThrottlingException : Exception
    {
        public ThrottlingException(string message) : base(message)
        {
        }
    }
}