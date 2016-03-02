using System.IO;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using IStorageClient = Knapcode.ToStorage.Core.AzureBlobStorage.IClient;

namespace Knapcode.ConnectorRide.Core
{
    public interface IGtfsFeedArchiveRecorder
    {
        Task<UploadResult> RecordAsync(ScrapeResult scrapeResult, bool groupAmPm, RecordRequest request);
        Task<Stream> GetLatestAsync(RecordRequest request);
    }

    public class GtfsFeedArchiveRecorder : IGtfsFeedArchiveRecorder
    {
        private readonly IStorageClient _storageClient;
        private readonly IGtfsConverter _converter;
        private readonly IGtfsFeedSerializer _serializer;

        public GtfsFeedArchiveRecorder(IStorageClient storageClient, IGtfsConverter converter, IGtfsFeedSerializer serializer)
        {
            _storageClient = storageClient;
            _converter = converter;
            _serializer = serializer;
        }

        public async Task<UploadResult> RecordAsync(ScrapeResult scrapeResult, bool groupAmPm, RecordRequest request)
        {
            using (var memoryStream = new MemoryStream())
            {
                // convert the scrape result to a GTFS .zip file
                var gtfsFeed = _converter.ConvertToFeed(scrapeResult, groupAmPm);
                await _serializer.SerializeAsync(memoryStream, gtfsFeed);
                memoryStream.Seek(0, SeekOrigin.Begin);

                // upload the .zip file
                return await _storageClient.UploadAsync(new UploadRequest
                {
                    ConnectionString = request.StorageConnectionString,
                    Stream = memoryStream,
                    ContentType = "application/octet-stream",
                    PathFormat = request.PathFormat,
                    Container = request.StorageContainer,
                    UploadDirect = true,
                    UploadLatest = true,
                    Trace = TextWriter.Null
                });
            }
        }

        public async Task<Stream> GetLatestAsync(RecordRequest request)
        {
            var getLatestRequest = new GetLatestRequest
            {
                ConnectionString = request.StorageConnectionString,
                PathFormat = request.PathFormat,
                Container = request.StorageContainer,
                Trace = TextWriter.Null
            };

            var result = await _storageClient.GetLatestStreamAsync(getLatestRequest);
            return result?.Stream;
        }
    }
}
