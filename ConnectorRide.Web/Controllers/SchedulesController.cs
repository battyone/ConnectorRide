using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Knapcode.ConnectorRide.Core;
using Knapcode.ConnectorRide.Core.Abstractions;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Client = Knapcode.ConnectorRide.Core.Client;
using StorageClient = Knapcode.ToStorage.Core.AzureBlobStorage.Client;
using StorageSystemTime = Knapcode.ToStorage.Core.Abstractions.SystemTime;

namespace Knapcode.ConnectorRide.Web.Controllers
{
    public class SchedulesController : ApiController
    {
        private readonly IThrottledScrapeResultRecorder _throttledScrapeResultRecorder;
        private readonly ISettings _settings;
        private readonly IScrapeResultRecorder _scrapeResultRecorder;
        private readonly IGtfsFeedArchiveRecorder _gtfsFeedArchiveRecord;

        public SchedulesController()
        {
            var systemTime = new SystemTime();
            var httpClient = new HttpClient();
            var client = new Client(httpClient);
            var scraper = new Scraper(systemTime, client);
            var serializer = new ScrapeResultSerializer();
            var storageSystemTime = new StorageSystemTime();
            var storageClient = new StorageClient(storageSystemTime);
            _scrapeResultRecorder = new ScrapeResultRecorder(scraper, serializer, storageClient);
            _throttledScrapeResultRecorder = new ThrottledScrapeResultRecorder(systemTime, _scrapeResultRecorder);
            var gtfsConverter = new GtfsConverter();
            var gtfsCsvSerializer = new GtfsCsvSerializer();
            var gtfsFeedSerializer = new GtfsFeedSerializer(gtfsCsvSerializer);
            _gtfsFeedArchiveRecord = new GtfsFeedArchiveRecorder(storageClient, gtfsConverter, gtfsFeedSerializer);
            var settingsService = new SettingsProvider();
            _settings = new Settings(settingsService);
        }

        public async Task<HttpResponseMessage> GetLatestGtfsFeedArchiveAsync()
        {
            var request = GetGtfsFeedArchiveRequest();
            var stream = await _gtfsFeedArchiveRecord.GetLatestAsync(request);

            if (stream == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("The GTFS feed archive has not been created yet.", Encoding.UTF8, "text/plain")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
                {
                    Headers =
                    {
                        ContentType = MediaTypeHeaderValue.Parse("application/octet-stream")
                    }
                }
            };
        }

        public async Task<ScrapeResult> GetLatestScrapeResultAsync()
        {
            var request = GetScrapeResultRequest();
            var latest = await _scrapeResultRecorder.GetLatestAsync(request);
            if (latest == null)
            {
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("The scrape result has not been created yet.", Encoding.UTF8, "text/plain")
                });
            }

            return latest;
        }

        public async Task<UploadResult> UpdateScrapeResultAsync()
        {
            var request = GetScrapeResultRequest();
            return await _throttledScrapeResultRecorder.RecordLatestAsync(_settings.SchedulesMaximumScrapeFrequency, request);
        }

        public async Task<UploadResult> UpdateGtfsFeedArchiveAsync()
        {
            var scrapeResult = await GetLatestScrapeResultAsync();
            var request = GetGtfsFeedArchiveRequest();
            return await _gtfsFeedArchiveRecord.RecordAsync(scrapeResult, request);
        }

        private RecordRequest GetScrapeResultRequest()
        {
            return new RecordRequest
            {
                PathFormat = _settings.ScrapeResultPathFormat,
                StorageConnectionString = _settings.StorageConnectionString,
                StorageContainer = _settings.StorageContainer
            };
        }

        private RecordRequest GetGtfsFeedArchiveRequest()
        {
            return new RecordRequest
            {
                PathFormat = _settings.GtfsFeedArchivePathFormat,
                StorageConnectionString = _settings.StorageConnectionString,
                StorageContainer = _settings.StorageContainer
            };
        }
    }
}
