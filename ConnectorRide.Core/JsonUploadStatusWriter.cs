using Knapcode.ConnectorRide.Core.RecorderModels;
using Newtonsoft.Json;

namespace Knapcode.ConnectorRide.Core
{
    public class JsonUploadStatusWriter : IUploadStatusWriter
    {
        private readonly JsonWriter _writer;

        public JsonUploadStatusWriter(JsonWriter writer)
        {
            _writer = writer;
        }

        public void Write(UploadStatus uploadStatus)
        {
            _writer.WriteValue(uploadStatus);
        }
    }
}