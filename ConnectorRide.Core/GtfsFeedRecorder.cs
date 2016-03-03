using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.RecorderModels;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Knapcode.ToStorage.Core;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using IStorageClient = Knapcode.ToStorage.Core.AzureBlobStorage.IClient;

namespace Knapcode.ConnectorRide.Core
{
    public interface IGtfsFeedArchiveRecorder
    {
        Task<UploadResult> RecordAsync(ScrapeResult scrapeResult, bool groupAmPm, RecordRequest request);
        Task<Stream> GetLatestAsync(RecordRequest request);
    }

    public class GtfsFeedArchiveRecorder : IGtfsFeedArchiveRecorder
    {
        private readonly IStorageClient _storageClient;
        private readonly IUniqueClient _uniqueClient;
        private readonly IGtfsConverter _converter;
        private readonly IGtfsFeedSerializer _serializer;

        public GtfsFeedArchiveRecorder(IStorageClient storageClient, IUniqueClient uniqueClient, IGtfsConverter converter, IGtfsFeedSerializer serializer)
        {
            _storageClient = storageClient;
            _uniqueClient = uniqueClient;
            _converter = converter;
            _serializer = serializer;
        }

        public async Task<UploadResult> RecordAsync(ScrapeResult scrapeResult, bool groupAmPm, RecordRequest request)
        {
            using (var resultStream = new MemoryStream())
            {
                // convert the scrape result to a GTFS .zip file
                var gtfsFeed = _converter.ConvertToFeed(scrapeResult, groupAmPm);
                await _serializer.SerializeAsync(resultStream, gtfsFeed);
                resultStream.Seek(0, SeekOrigin.Begin);

                var uploadRequest = new UniqueUploadRequest
                {
                    ConnectionString = request.StorageConnectionString,
                    Stream = resultStream,
                    ContentType = "application/octet-stream",
                    PathFormat = request.PathFormat,
                    Container = request.StorageContainer,
                    UploadDirect = true,
                    Trace = TextWriter.Null,
                    EqualsAsync = async x =>
                    {
                        var equals = await ZipArchiveEqualsAsync(resultStream, x.Stream);
                        resultStream.Seek(0, SeekOrigin.Begin);
                        return equals;
                    }
                };

                // upload the .zip file
                return await _uniqueClient.UploadAsync(uploadRequest);
            }
        }

        public async Task<Stream> GetLatestAsync(RecordRequest request)
        {
            var getLatestRequest = new GetLatestRequest
            {
                ConnectionString = request.StorageConnectionString,
                PathFormat = request.PathFormat,
                Container = request.StorageContainer,
                Trace = TextWriter.Null
            };

            var result = await _storageClient.GetLatestStreamAsync(getLatestRequest);
            return result?.Stream;
        }

        private async Task<bool> ZipArchiveEqualsAsync(Stream x, Stream y)
        {
            var streamComparer = new AsyncStreamEqualityComparer();
            using (var zipX = new ZipArchive(x, ZipArchiveMode.Read, true))
            using (var zipY = new ZipArchive(y, ZipArchiveMode.Read, true))
            {
                var entriesX = new HashSet<string>(zipX.Entries.Select(e => e.FullName));
                var entriesY = new HashSet<string>(zipY.Entries.Select(e => e.FullName));
                if (!entriesX.SetEquals(entriesY))
                {
                    return false;
                }

                foreach (var entry in entriesX)
                {
                    using (var entryX = zipX.GetEntry(entry).Open())
                    using (var entryY = zipY.GetEntry(entry).Open())
                    {
                        if (!await streamComparer.EqualsAsync(entryX, entryY, CancellationToken.None))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
