using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.Abstractions;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Knapcode.ConnectorRide.Core
{
    public interface IScraper
    {
        Task<Result> ScrapeAsync();
        Task RealTimeScrapeAsync(TextWriter textWriter);
    }

    public class Scraper : IScraper
    {
        private const string Version = "3.0.0";
        private readonly ISystemTime _systemTime;
        private readonly IClient _client;

        public Scraper(ISystemTime systemTime, IClient client)
        {
            _systemTime = systemTime;
            _client = client;
        }

        public async Task<Result> ScrapeAsync()
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

            return new Result
            {
                Version = Version,
                StartTime = startTime,
                Schedules = schedules,
                EndTime = _systemTime.UtcNow
            };
        }
        
        public async Task RealTimeScrapeAsync(TextWriter textWriter)
        {
            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("Version");
                jsonWriter.WriteValue("3.0.0");
                jsonWriter.WritePropertyName("StartTime");
                jsonWriter.WriteValue(DateTimeOffset.UtcNow);
                jsonWriter.WritePropertyName("Schedules");
                jsonWriter.WriteStartArray();
                
                var scheduleReferences = await _client.GetScheduleReferencesAsync().ConfigureAwait(false);
                foreach (var scheduleReference in scheduleReferences)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("Name");
                    var schedule = await _client.GetScheduleAsync(scheduleReference).ConfigureAwait(false);
                    jsonWriter.WriteValue(schedule.Name);
                    jsonWriter.WritePropertyName("Table");
                    JObject.FromObject(schedule.Table).WriteTo(jsonWriter);

                    jsonWriter.WritePropertyName("Map");
                    var map = await _client.GetMapAsync(schedule.MapReference).ConfigureAwait(false);
                    JObject.FromObject(map).WriteTo(jsonWriter);
                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndArray();
                jsonWriter.WritePropertyName("EndTime");
                jsonWriter.WriteValue(DateTimeOffset.UtcNow);
                jsonWriter.WriteEndObject();
            }
        }
    }
}
