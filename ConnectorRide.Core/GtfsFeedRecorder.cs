using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using IStorageClient = Knapcode.ToStorage.Core.AzureBlobStorage.IClient;

namespace Knapcode.ConnectorRide.Core
{
    public interface IGtfsFeedArchiveRecorder
    {
        Task<RecordResult> RecordAsync(ScrapeResult scrapeResult, bool groupAmPm, RecordRequest request);
        Task<Stream> GetLatestAsync(RecordRequest request);
    }

    public class GtfsFeedArchiveRecorder : IGtfsFeedArchiveRecorder
    {
        private readonly IStorageClient _storageClient;
        private readonly IUniqueClient _uniqueClient;
        private readonly IGtfsConverter _converter;
        private readonly IGtfsFeedSerializer _serializer;
        private readonly IUploadStatusRecorder _statusRecorder;

        public GtfsFeedArchiveRecorder(IStorageClient storageClient, IUniqueClient uniqueClient, IGtfsConverter converter, IGtfsFeedSerializer serializer, IUploadStatusRecorder statusRecorder)
        {
            _storageClient = storageClient;
            _uniqueClient = uniqueClient;
            _converter = converter;
            _serializer = serializer;
            _statusRecorder = statusRecorder;
        }

        public async Task<RecordResult> RecordAsync(ScrapeResult scrapeResult, bool groupAmPm, RecordRequest request)
        {
            using (var resultStream = new MemoryStream())
            {
                // convert the scrape result to a GTFS .zip file
                var gtfsFeed = _converter.ConvertToFeed(scrapeResult, groupAmPm);
                await _serializer.SerializeAsync(resultStream, gtfsFeed);
                resultStream.Seek(0, SeekOrigin.Begin);

                var uploadRequest = new UniqueUploadRequest
                {
                    ConnectionString = request.StorageConnectionString,
                    Stream = resultStream,
                    ContentType = "application/octet-stream",
                    PathFormat = request.BlobPathFormat,
                    Container = request.StorageContainer,
                    UploadDirect = true,
                    Trace = TextWriter.Null,
                    EqualsAsync = async x =>
                    {
                        var comparer = new ZipArchiveEqualityComparer();
                        var equals = await comparer.EqualsAsync(resultStream, x.Stream, CancellationToken.None);
                        resultStream.Seek(0, SeekOrigin.Begin);
                        return equals;
                    }
                };

                // upload the .zip file
                var blobUploadResult = await _uniqueClient.UploadAsync(uploadRequest);

                // record status
                var statusUploadResult = await _statusRecorder.RecordStatusAsync(blobUploadResult, request);

                return new RecordResult
                {
                    BlobUploadResult = blobUploadResult,
                    StatusUploadResult = statusUploadResult
                };
            }
        }

        public async Task<Stream> GetLatestAsync(RecordRequest request)
        {
            var getLatestRequest = new GetLatestRequest
            {
                ConnectionString = request.StorageConnectionString,
                PathFormat = request.BlobPathFormat,
                Container = request.StorageContainer,
                Trace = TextWriter.Null
            };

            var result = await _storageClient.GetLatestStreamAsync(getLatestRequest);
            return result?.Stream;
        }
    }
}
