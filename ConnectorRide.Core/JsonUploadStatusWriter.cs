using Knapcode.ConnectorRide.Core.RecorderModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            JObject.FromObject(uploadStatus).WriteTo(_writer);
        }
    }
}