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
        private readonly ThrottledRecorder _throttledRecorder;
        private readonly ConnectorRideSettings _settings;
        private readonly Recorder _recorder;

        public SchedulesController()
        {
            var systemTime = new SystemTime();
            var httpClient = new HttpClient();
            var client = new Client(httpClient);
            var scraper = new Scraper(systemTime, client);
            var serializer = new ScrapeResultSerializer();
            var storageSystemTime = new StorageSystemTime();
            var storageClient = new StorageClient(storageSystemTime);
            _recorder = new Recorder(scraper, serializer, storageClient);
            _throttledRecorder = new ThrottledRecorder(systemTime, _recorder);

            var settingsService = new SettingsService();
            _settings = new ConnectorRideSettings(settingsService);
        }

        public async Task<ScrapeResult> GetLatestSchedulesAsync()
        {
            return await _recorder.GetLatestAsync(GetRecordRequest());
        }

        public async Task<UploadResult> UpdateSchedulesAsync()
        {

            return await _throttledRecorder.RecordLatestAsync(new ThrottledRecordRequest
            {
                MaximumFrequency = _settings.SchedulesMaximumScrapeFrequency,
                Request = GetRecordRequest()
            });
        }

        private RecordRequest GetRecordRequest()
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
