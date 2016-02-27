namespace Knapcode.ConnectorRide.Core.RecorderModels
{
    public class ThrottlingException : ConnectorRideException
    {
        public ThrottlingException(string message) : base(message)
        {
        }
    }
}