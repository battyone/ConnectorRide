using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Knapcode.ConnectorRide.Core;
using Knapcode.ConnectorRide.Core.Abstractions;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Knapcode.ConnectorRide.Web.Settings;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Client = Knapcode.ConnectorRide.Core.Client;
using StorageClient = Knapcode.ToStorage.Core.AzureBlobStorage.Client;
using StorageSystemTime = Knapcode.ToStorage.Core.Abstractions.SystemTime;

namespace Knapcode.ConnectorRide.Web.Controllers
{
    public class SchedulesController : ApiController
    {
        private readonly ThrottledScrapeResultRecorder _throttledScrapeResultRecorder;
        private readonly ConnectorRideSettings _settings;
        private readonly ScrapeResultRecorder _scrapeResultRecorder;

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

            var settingsService = new SettingsService();
            _settings = new ConnectorRideSettings(settingsService);
        }

        public async Task<ScrapeResult> GetLatestScrapeResult()
        {
            var request = GetScrapeResultRecordRequest();

            if (request == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return await _scrapeResultRecorder.GetLatestAsync(request);
        }

        public async Task<UploadResult> UpdateScrapeResultAsync()
        {
            var request = GetScrapeResultRecordRequest();
            return await _throttledScrapeResultRecorder.RecordLatestAsync(_settings.SchedulesMaximumScrapeFrequency, request);
        }

        private RecordRequest GetScrapeResultRecordRequest()
        {
            return new RecordRequest
            {
                PathFormat = _settings.SchedulesPathFormat,
                StorageConnectionString = _settings.StorageConnectionString,
                StorageContainer = _settings.StorageContainer
            };
        }
    }
}
