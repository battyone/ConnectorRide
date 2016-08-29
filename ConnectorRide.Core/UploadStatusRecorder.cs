using System.IO;
using System.Text;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ToStorage.Core.Abstractions;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Newtonsoft.Json;
using IStorageClient = Knapcode.ToStorage.Core.AzureBlobStorage.IClient;

namespace Knapcode.ConnectorRide.Core
{
    public class UploadStatusRecorder : IUploadStatusRecorder
    {
        private readonly IStorageClient _storageClient;
        private readonly ISystemTime _systemTime;

        public UploadStatusRecorder(IStorageClient storageClient, ISystemTime systemTime)
        {
            _storageClient = storageClient;
            _systemTime = systemTime;
        }

        public async Task<UploadResult> RecordStatusAsync(UploadResult result, RecordRequest request)
        {
            var statusStream = new MemoryStream();
            using (var textWriter = new StreamWriter(statusStream, new UTF8Encoding(false), 4096, true))
            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                var writer = new JsonUploadStatusWriter(jsonWriter);

                writer.Write(new UploadStatus
                {
                    UploadResult = result,
                    Time = _systemTime.UtcNow
                });
            }
            
            statusStream.Seek(0, SeekOrigin.Begin);

            var uploadRequest = new UploadRequest
            {
                ConnectionString = request.StorageConnectionString,
                Container = request.StorageContainer,
                ContentType = "application/json",
                PathFormat = request.StatusPathFormat,
                Stream = statusStream,
                Trace = TextWriter.Null,
                UploadDirect = false,
                UploadLatest = true
            };

            // upload
            using (statusStream)
            {
                return await _storageClient.UploadAsync(uploadRequest);
            }
        }
    }
}
