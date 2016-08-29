using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ConnectorRide.Core
{
    public interface IUploadStatusRecorder
    {
        Task<UploadResult> RecordStatusAsync(UploadResult result, RecordRequest request);
    }
}