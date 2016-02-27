using System.IO;
using System.Text;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using IStorageClient = Knapcode.ToStorage.Core.AzureBlobStorage.IClient;

namespace Knapcode.ConnectorRide.Core
{
    public interface IRecorder
    {
        Task<UploadResult> RecordLatestAsync(RecordRequest request);
        Task<ScrapeResult> GetLatestAsync(RecordRequest request);
    }

    public class Recorder : IRecorder
    {
        private readonly IScraper _scraper;
        private readonly ISerializer _serializer;
        private readonly IStorageClient _storageClient;

        public Recorder(IScraper scraper, ISerializer serializer, IStorageClient storageClient)
        {
            _scraper = scraper;
            _serializer = serializer;
            _storageClient = storageClient;
        }
        
        public async Task<ScrapeResult> GetLatestAsync(RecordRequest request)
        {
            var getLatestRequest = new GetLatestRequest
            {
                PathFormat = request.PathFormat,
                Container = request.StorageContainer,
                Trace = TextWriter.Null
            };

            using (var stream = await _storageClient.GetLatestStreamAsync(request.StorageConnectionString, getLatestRequest))
            {
                return await _serializer.DeserializeScrapeResultAsync(stream);
            }
        }

        public async Task<UploadResult> RecordLatestAsync(RecordRequest request)
        {
            // scrape
            var resultStream = new MemoryStream();
            using (var writer = new StreamWriter(resultStream, new UTF8Encoding(false), 4096, true))
            {
                await _scraper.RealTimeScrapeAsync(writer).ConfigureAwait(false);
            }

            resultStream.Position = 0;

            // initialize storage
            var uploadRequest = new UploadRequest
            {
                Container = request.StorageContainer,
                ContentType = "application/json",
                PathFormat = request.PathFormat,
                Stream = resultStream,
                Trace = TextWriter.Null,
                UploadDirect = true,
                UploadLatest = true
            };

            // upload
            using (resultStream)
            {
                return await _storageClient.UploadAsync(request.StorageConnectionString, uploadRequest).ConfigureAwait(false);
            }
        }
    }
}
