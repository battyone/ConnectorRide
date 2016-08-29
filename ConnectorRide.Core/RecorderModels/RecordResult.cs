using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ConnectorRide.Core.RecorderModels
{
    public class RecordResult
    {
        public UploadResult BlobUploadResult { get; set; }
        public UploadResult StatusUploadResult { get; set; }
    }
}
