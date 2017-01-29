using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Knapcode.ConnectorRide.Common.Models;
using Knapcode.ConnectorRide.Core;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ToStorage.Core.Abstractions;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Client = Knapcode.ConnectorRide.Core.Client;
using StorageClient = Knapcode.ToStorage.Core.AzureBlobStorage.Client;

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
            var pathBuilder = new PathBuilder();
            var storageClient = new StorageClient(systemTime, pathBuilder);
            var uniqueClient = new UniqueClient(storageClient);
            var statusRecorder = new UploadStatusRecorder(storageClient, systemTime);
            _scrapeResultRecorder = new ScrapeResultRecorder(scraper, serializer, storageClient, uniqueClient, statusRecorder);
            _throttledScrapeResultRecorder = new ThrottledScrapeResultRecorder(systemTime, _scrapeResultRecorder);
            var gtfsConverter = new GtfsConverter();
            var gtfsCsvSerializer = new GtfsCsvSerializer();
            var gtfsFeedSerializer = new GtfsFeedSerializer(gtfsCsvSerializer);
            _gtfsFeedArchiveRecord = new GtfsFeedArchiveRecorder(storageClient, uniqueClient, gtfsConverter, gtfsFeedSerializer, statusRecorder);
            var settingsService = new SettingsProvider();
            _settings = new Settings(settingsService);
        }

        public async Task<HttpResponseMessage> GetLatestGtfsFeedArchiveGroupedAsync()
        {
            return await GetLatestGtfsFeedArchiveAsync(true);
        }

        public async Task<RecordResult> UpdateGtfsFeedArchiveGroupedAsync()
        {
            return await UpdateGtfsFeedArchiveAsync(true);
        }

        public async Task<HttpResponseMessage> GetLatestGtfsFeedArchiveUnroupedAsync()
        {
            return await GetLatestGtfsFeedArchiveAsync(false);
        }

        public async Task<RecordResult> UpdateGtfsFeedArchiveUngroupedAsync()
        {
            return await UpdateGtfsFeedArchiveAsync(false);
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

        public async Task<RecordResult> UpdateScrapeResultAsync()
        {
            var request = GetScrapeResultRequest();
            return await _throttledScrapeResultRecorder.RecordLatestAsync(_settings.SchedulesMaximumScrapeFrequency, request);
        }

        private async Task<HttpResponseMessage> GetLatestGtfsFeedArchiveAsync(bool groupAmPm)
        {
            var request = GetGtfsFeedArchiveRequest(groupAmPm);
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

        private async Task<RecordResult> UpdateGtfsFeedArchiveAsync(bool groupAmPm)
        {
            var scrapeResult = await GetLatestScrapeResultAsync();
            var request = GetGtfsFeedArchiveRequest(groupAmPm);
            return await _gtfsFeedArchiveRecord.RecordAsync(scrapeResult, groupAmPm, request);
        }

        private RecordRequest GetScrapeResultRequest()
        {
            return new RecordRequest
            {
                BlobPathFormat = _settings.ScrapeResultPathFormat,
                StatusPathFormat = _settings.ScrapeResultStatusPathFormat,
                StorageConnectionString = _settings.StorageConnectionString,
                StorageContainer = _settings.StorageContainer
            };
        }

        private RecordRequest GetGtfsFeedArchiveRequest(bool groupAmPm)
        {
            var pathFormat = groupAmPm ? _settings.GtfsFeedArchiveGroupedPathFormat : _settings.GtfsFeedArchiveUngroupedPathFormat;
            var statusPathFormat = groupAmPm ? _settings.GtfsFeedArchiveGroupedStatusPathFormat : _settings.GtfsFeedArchiveUngroupedStatusPathFormat;

            return new RecordRequest
            {
                BlobPathFormat = pathFormat,
                StatusPathFormat = statusPathFormat,
                StorageConnectionString = _settings.StorageConnectionString,
                StorageContainer = _settings.StorageContainer
            };
        }
    }
}
