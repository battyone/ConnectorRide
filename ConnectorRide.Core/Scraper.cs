using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Knapcode.ConnectorRide.Core
{
    public class Scraper
    {
        private readonly Client _client;

        public Scraper(Client client)
        {
            _client = client;
        }

        public async Task<ScraperResult> ScrapeAsync()
        {
            var startTime = DateTimeOffset.UtcNow;

            var scheduleReferences = await _client.GetSchedulesAsync().ConfigureAwait(false);
            var schedules = new List<Schedule>();
            foreach (var scheduleReference in scheduleReferences)
            {
                var schedule = await _client.GetScheduleAsync(scheduleReference).ConfigureAwait(false);
                schedules.Add(schedule);
            }

            return new ScraperResult
            {
                StartTime = startTime,
                Schedules = schedules,
                EndTime = DateTimeOffset.UtcNow
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
                
                var scheduleReferences = await _client.GetSchedulesAsync().ConfigureAwait(false);
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
