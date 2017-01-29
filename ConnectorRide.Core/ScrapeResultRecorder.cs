using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Common.Models;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Newtonsoft.Json;
using IStorageClient = Knapcode.ToStorage.Core.AzureBlobStorage.IClient;

namespace Knapcode.ConnectorRide.Core
{
    public interface IScrapeResultRecorder
    {
        Task<RecordResult> RecordAsync(RecordRequest request);
        Task<ScrapeResult> GetLatestAsync(RecordRequest request);
    }

    public class ScrapeResultRecorder : IScrapeResultRecorder
    {
        private readonly IScraper _scraper;
        private readonly IScrapeResultSerializer _serializer;
        private readonly IUploadStatusRecorder _statusRecorder;
        private readonly IStorageClient _storageClient;
        private readonly IUniqueClient _uniqueClient;

        public ScrapeResultRecorder(IScraper scraper, IScrapeResultSerializer serializer, IStorageClient storageClient, IUniqueClient uniqueClient, IUploadStatusRecorder statusRecorder)
        {
            _scraper = scraper;
            _serializer = serializer;
            _storageClient = storageClient;
            _uniqueClient = uniqueClient;
            _statusRecorder = statusRecorder;
        }
        
        public async Task<ScrapeResult> GetLatestAsync(RecordRequest request)
        {
            var getLatestRequest = new GetLatestRequest
            {
                ConnectionString = request.StorageConnectionString,
                PathFormat = request.BlobPathFormat,
                Container = request.StorageContainer,
                Trace = TextWriter.Null
            };

            using (var streamResult = await _storageClient.GetLatestStreamAsync(getLatestRequest))
            {
                if (streamResult == null)
                {
                    return null;
                }

                return await _serializer.DeserializeAsync(streamResult.Stream, false);
            }
        }

        public async Task<RecordResult> RecordAsync(RecordRequest request)
        {
            // scrape
            var resultStream = new MemoryStream();
            using (var textWriter = new StreamWriter(resultStream, new UTF8Encoding(false), 4096, true))
            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                var writer = new JsonScrapeResultWriter(jsonWriter);
                await _scraper.RealTimeScrapeAsync(writer);
            }

            resultStream.Seek(0, SeekOrigin.Begin);

            // initialize storage
            var blobUploadRequest = new UniqueUploadRequest
            {
                ConnectionString = request.StorageConnectionString,
                Container = request.StorageContainer,
                ContentType = "application/json",
                PathFormat = request.BlobPathFormat,
                Stream = resultStream,
                Trace = TextWriter.Null,
                UploadDirect = true,
                EqualsAsync = async x =>
                {
                    var comparer = new ScrapeResultEqualityComparer();
                    var equals = await comparer.EqualsAsync(x.Stream, resultStream, CancellationToken.None);
                    resultStream.Seek(0, SeekOrigin.Begin);
                    return equals;
                }
            };

            // upload
            UploadResult blobUploadResult;
            using (resultStream)
            {
                blobUploadResult =  await _uniqueClient.UploadAsync(blobUploadRequest);
            }

            // record status
            var statusUploadResult = await _statusRecorder.RecordStatusAsync(blobUploadResult, request);

            return new RecordResult
            {
                BlobUploadResult = blobUploadResult,
                StatusUploadResult = statusUploadResult
            };
        }
    }
}
