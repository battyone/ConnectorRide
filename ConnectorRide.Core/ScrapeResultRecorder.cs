﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ConnectorRide.Core.ScraperModels;
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

        public ScrapeResultRecorder(IScraper scraper, IScrapeResultSerializer serializer, IStorageClient storageClient)
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
                if (stream == null)
                {
                    return null;
                }

                return await _serializer.DeserializeAsync(stream);
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