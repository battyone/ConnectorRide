using System.IO;
using System.Text;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ConnectorRide.Core
{
    public interface IRecorder
    {
        Task<UploadResult> RecordLatestAsync(RecordRequest request);
    }

    public class Recorder : IRecorder
    {
        private readonly IScraper _scraper;

        public Recorder(IScraper scraper)
        {
            _scraper = scraper;
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
            var storageClient = new ToStorage.Core.AzureBlobStorage.Client();
            var uploadRequest = new UploadRequest
            {
                Container = request.StorageContainer,
                ContentType = "application/json",
                PathFormat = request.PathFormat,
                Stream = resultStream,
                Trace = TextWriter.Null,
                UpdateLatest = true
            };

            // upload
            using (resultStream)
            {
                return await storageClient.UploadAsync(request.StorageConnectionString, uploadRequest).ConfigureAwait(false);
            }
        }
    }
}
