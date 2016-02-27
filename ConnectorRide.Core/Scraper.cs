using System.Collections.Generic;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.Abstractions;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Schedule = Knapcode.ConnectorRide.Core.ScraperModels.Schedule;

namespace Knapcode.ConnectorRide.Core
{
    public interface IScraper
    {
        Task<ScrapeResult> ScrapeAsync();
        Task RealTimeScrapeAsync(IScrapeResultWriter writer);
    }

    public class Scraper : IScraper
    {
        private const string Version = "3.1.0";
        private readonly ISystemTime _systemTime;
        private readonly IClient _client;

        public Scraper(ISystemTime systemTime, IClient client)
        {
            _systemTime = systemTime;
            _client = client;
        }

        public async Task<ScrapeResult> ScrapeAsync()
        {
            var startTime = _systemTime.UtcNow;

            var scheduleReferences = await _client.GetScheduleReferencesAsync().ConfigureAwait(false);
            var schedules = new List<Schedule>();

            foreach (var scheduleReference in scheduleReferences)
            {
                var clientSchedule = await _client.GetScheduleAsync(scheduleReference).ConfigureAwait(false);
                var map = await _client.GetMapAsync(clientSchedule.MapReference).ConfigureAwait(false);

                schedules.Add(new Schedule
                {
                    Name = clientSchedule.Name,
                    Table = clientSchedule.Table,
                    Map = map
                });
            }

            return new ScrapeResult
            {
                Version = Version,
                StartTime = startTime,
                Schedules = schedules,
                EndTime = _systemTime.UtcNow
            };
        }
        
        public async Task RealTimeScrapeAsync(IScrapeResultWriter writer)
        {
            writer.WriteStart();
            writer.WriteVersion(Version);
            writer.WriteStartTime(_systemTime.UtcNow);
            writer.WriteStartSchedules();

            var scheduleReferences = await _client.GetScheduleReferencesAsync().ConfigureAwait(false);
            foreach (var scheduleReference in scheduleReferences)
            {
                writer.WriteStartSchedule();

                var schedule = await _client.GetScheduleAsync(scheduleReference).ConfigureAwait(false);
                writer.WriteScheduleName(schedule.Name);
                writer.WriteScheduleTable(schedule.Table);

                var map = await _client.GetMapAsync(schedule.MapReference).ConfigureAwait(false);
                writer.WriteScheduleMap(map);
                writer.WriteEndSchedule();
            }

            writer.WriteEndSchedules();
            writer.WriteEndTime(_systemTime.UtcNow);
            writer.WriteEnd();
        }
    }
}
