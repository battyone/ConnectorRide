using System;
using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ConnectorRide.Core.RecorderModels
{
    public class UploadStatus
    {
        public DateTimeOffset Time { get; set; }
        public UploadResult UploadResult { get; set; }
    }
}
