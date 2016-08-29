using Knapcode.ConnectorRide.Core.RecorderModels;

namespace Knapcode.ConnectorRide.Core
{
    public interface IUploadStatusWriter
    {
        void Write(UploadStatus uploadStatus);
    }
}