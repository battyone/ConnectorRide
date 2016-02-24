namespace Knapcode.ConnectorRide.Core.RecorderModels
{
    public class RecordRequest
    {
        public string StorageConnectionString { get; set; }
        public string StorageContainer { get; set; }
        public string PathFormat { get; set; }
    }
}