namespace Knapcode.ConnectorRide.Core
{
    public class ThrottlingException : ConnectorRideException
    {
        public ThrottlingException(string message) : base(message)
        {
        }
    }
}