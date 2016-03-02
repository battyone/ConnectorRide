using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Knapcode.ToStorage.Core;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Newtonsoft.Json;
using IStorageClient = Knapcode.ToStorage.Core.AzureBlobStorage.IClient;

namespace Knapcode.ConnectorRide.Core
{
    public interface IScrapeResultRecorder
    {
        Task<UploadResult> RecordAsync(RecordRequest request);
        Task<ScrapeResult> GetLatestAsync(RecordRequest request);
    }

    public class ScrapeResultRecorder : IScrapeResultRecorder
    {
        private readonly IScraper _scraper;
        private readonly IScrapeResultSerializer _serializer;
        private readonly IStorageClient _storageClient;
        private readonly IUniqueClient _uniqueClient;

        public ScrapeResultRecorder(IScraper scraper, IScrapeResultSerializer serializer, IStorageClient storageClient, IUniqueClient uniqueClient)
        {
            _scraper = scraper;
            _serializer = serializer;
            _storageClient = storageClient;
            _uniqueClient = uniqueClient;
        }
        
        public async Task<ScrapeResult> GetLatestAsync(RecordRequest request)
        {
            var getLatestRequest = new GetLatestRequest
            {
                ConnectionString = request.StorageConnectionString,
                PathFormat = request.PathFormat,
                Container = request.StorageContainer,
                Trace = TextWriter.Null
            };

            using (var streamResult = await _storageClient.GetLatestStreamAsync(getLatestRequest))
            {
                if (streamResult == null)
                {
                    return null;
                }

                return await _serializer.DeserializeAsync(streamResult.Stream);
            }
        }

        public async Task<UploadResult> RecordAsync(RecordRequest request)
        {
            // scrape
            var resultStream = new MemoryStream();
            using (var textWriter = new StreamWriter(resultStream, new UTF8Encoding(false), 4096, true))
            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                var writer = new JsonScrapeResultWriter(jsonWriter);
                await _scraper.RealTimeScrapeAsync(writer).ConfigureAwait(false);
            }

            resultStream.Seek(0, SeekOrigin.Begin);

            // initialize storage
            var uploadRequest = new UniqueUploadRequest
            {
                ConnectionString = request.StorageConnectionString,
                Container = request.StorageContainer,
                ContentType = "application/json",
                PathFormat = request.PathFormat,
                Stream = resultStream,
                Trace = TextWriter.Null,
                UploadDirect = true,
                IsUniqueAsync = async x =>
                {
                    var equals = await new AsyncStreamEqualityComparer().EqualsAsync(resultStream, x.Stream, CancellationToken.None);
                    resultStream.Seek(0, SeekOrigin.Begin);
                    return equals;
                }
            };

            // upload
            using (resultStream)
            {
                return await _uniqueClient.UploadAsync(uploadRequest).ConfigureAwait(false);
            }
        }
    }
}
