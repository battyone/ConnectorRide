using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core;
using Knapcode.ConnectorRide.Web.Settings;
using Knapcode.SocketToMe.Http;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Client = Knapcode.ConnectorRide.Core.Client;

namespace Knapcode.ConnectorRide.Web.Services
{
    public class CommandsService
    {
        public static SemaphoreSlim UpdateLock = new SemaphoreSlim(1);
        public static DateTimeOffset LastUpdate = DateTimeOffset.MinValue;

        public async Task<UploadResult> UpdateSchedulesAsync()
        {
            var acquired = await UpdateLock.WaitAsync(0).ConfigureAwait(false);
            if (!acquired)
            {
                throw new ThrottlingException("An update is already running.");
            }

            try
            {
                // initialize configuration
                var settings = new ConnectorRideSettings(new SettingsService());

                // throttle
                if (DateTimeOffset.UtcNow - LastUpdate < settings.SchedulesMaximumScrapeFrequency)
                {
                    throw new ThrottlingException("An update occurred too recently.");
                }

                // initialize scraper
                var handler = new NetworkHandler();
                var httpClient = new HttpClient(handler);
                var connectorClient = new Client(httpClient);
                var connectorScraper = new Scraper(connectorClient);

                // scrape
                var resultStream = new MemoryStream();
                using (var writer = new StreamWriter(resultStream, new UTF8Encoding(false), 4096, true))
                {
                    await connectorScraper.RealTimeScrapeAsync(writer).ConfigureAwait(false);
                }

                resultStream.Position = 0;

                // initialize storage
                var storageClient = new ToStorage.Core.AzureBlobStorage.Client();
                var uploadRequest = new UploadRequest
                {
                    Container = settings.StorageContainer,
                    ContentType = "application/json",
                    PathFormat = settings.SchedulesPathFormat,
                    Stream = resultStream,
                    Trace = TextWriter.Null,
                    UpdateLatest = true
                };

                // upload
                using (resultStream)
                {
                    var result = await storageClient.UploadAsync(settings.StorageConnectionString, uploadRequest).ConfigureAwait(false);
                    LastUpdate = DateTimeOffset.UtcNow;
                    return result;
                }
            }
            finally
            {
                UpdateLock.Release();
            }
        }
    }
}