using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Knapcode.ConnectorRide.Core;
using Knapcode.SocketToMe.Http;
using Knapcode.ToStorage.Core.AzureBlobStorage;

namespace Knapcode.ConnectorRide.Web.Controllers
{
    public class CommandsController : ApiController
    {
        public async Task<UploadResult> UpdateAsync()
        {
            // initialize configuration
            var settings = new StorageSettings(new SettingsService());

            // initialize scraper
            var handler = new NetworkHandler();
            var httpClient = new HttpClient(handler);
            var connectorClient = new ConnectorClient(httpClient);
            var connectorScraper = new ConnectorScraper(connectorClient);

            // scrape
            var resultStream = new MemoryStream();
            using (var writer = new StreamWriter(resultStream, new UTF8Encoding(false), 4096, true))
            {
                await connectorScraper.RealTimeScrapeAsync(writer);
            }

            resultStream.Position = 0;

            // initialize storage
            var storageClient = new Client();
            var uploadRequest = new UploadRequest
            {
                Container = settings.Container,
                ContentType = "application/json",
                PathFormat = settings.SchedulePathFormat,
                Stream = resultStream,
                Trace = TextWriter.Null,
                UpdateLatest = true
            };

            // upload
            using (resultStream)
            {
                return await storageClient.UploadAsync(settings.SchedulePathFormat, uploadRequest);
            }
        }
    }
}
