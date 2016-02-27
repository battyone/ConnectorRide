using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.GtfsModels;

namespace Knapcode.ConnectorRide.Core
{
    public class GtfsFeedSerializer
    {
        private readonly GtfsCsvSerializer _csvSerializer;

        public GtfsFeedSerializer()
        {
            _csvSerializer = new GtfsCsvSerializer();
        }

        public async Task SerializeAsync(Stream zipStream, GtfsFeed feed)
        {
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                using (var entryStream = archive.CreateEntry("agency.txt").Open())
                {
                    _csvSerializer.SerializeAgencies(entryStream, feed.Agencies);
                }

                using (var entryStream = archive.CreateEntry("stops.txt").Open())
                {
                    _csvSerializer.SerializeStops(entryStream, feed.Stops);
                }

                using (var entryStream = archive.CreateEntry("routes.txt").Open())
                {
                    _csvSerializer.SerializeRoutes(entryStream, feed.Routes);
                }

                using (var entryStream = archive.CreateEntry("trips.txt").Open())
                {
                    _csvSerializer.SerializeTrips(entryStream, feed.Trips);
                }

                using (var entryStream = archive.CreateEntry("stop_times.txt").Open())
                {
                    _csvSerializer.SerializeStopTimes(entryStream, feed.StopTimes);
                }

                using (var entryStream = archive.CreateEntry("calendar.txt").Open())
                {
                    _csvSerializer.SerializeCalendar(entryStream, feed.Calendar);
                }
            }
        }
    }
}