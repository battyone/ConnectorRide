using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Knapcode.ConnectorRide.Core;
using Knapcode.ConnectorRide.Core.Abstractions;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ConnectorRide.Web.Settings;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Client = Knapcode.ConnectorRide.Core.Client;

namespace Knapcode.ConnectorRide.Web.Controllers
{
    public class CommandsController : ApiController
    {
        public async Task<UploadResult> UpdateSchedulesAsync()
        {
            var systemTime = new SystemTime();
            var httpClient = new HttpClient();
            var client = new Client(httpClient);
            var scraper = new Scraper(systemTime, client);
            var recorder = new Recorder(scraper);
            var throttledRecorder = new ThrottledRecorder(systemTime, recorder);

            var settings = new ConnectorRideSettings(new SettingsService());

            return await throttledRecorder.RecordLatestAsync(new ThrottledRecordRequest
            {
                MaximumFrequency = settings.SchedulesMaximumScrapeFrequency,
                Request = new RecordRequest
                {
                    PathFormat = settings.SchedulesPathFormat,
                    StorageConnectionString = settings.StorageConnectionString,
                    StorageContainer = settings.StorageContainer
                }
            });
        }
    }
}
