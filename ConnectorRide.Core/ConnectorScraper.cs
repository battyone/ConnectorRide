using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Knapcode.ConnectorRide.Core
{
    public class ConnectorScraper
    {
        private readonly ConnectorClient _client;

        public ConnectorScraper(ConnectorClient client)
        {
            _client = client;
        }

        public async Task<ConnectorScrapeResult> ScrapeAsync()
        {
            var startTime = DateTimeOffset.UtcNow;

            var scheduleReferences = await _client.GetSchedulesAsync().ConfigureAwait(false);
            var schedules = new List<Schedule>();
            foreach (var scheduleReference in scheduleReferences)
            {
                var schedule = await _client.GetScheduleAsync(scheduleReference).ConfigureAwait(false);
                schedules.Add(schedule);
            }

            return new ConnectorScrapeResult
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
                var startTime = DateTimeOffset.UtcNow;
                var scheduleReferences = await _client.GetSchedulesAsync().ConfigureAwait(false);

                bool started = false;
                foreach (var scheduleReference in scheduleReferences)
                {
                    var schedule = await _client.GetScheduleAsync(scheduleReference).ConfigureAwait(false);

                    if (!started)
                    {
                        jsonWriter.WriteStartObject();
                        jsonWriter.WritePropertyName("Version");
                        jsonWriter.WriteValue("2.0.0");
                        jsonWriter.WritePropertyName("StartTime");
                        jsonWriter.WriteValue(startTime);
                        jsonWriter.WritePropertyName("Schedules");
                        jsonWriter.WriteStartArray();
                        started = true;
                    }

                    var json = JObject.FromObject(schedule);
                    json.WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndArray();
                jsonWriter.WritePropertyName("EndTime");
                jsonWriter.WriteValue(DateTimeOffset.UtcNow);
                jsonWriter.WriteEndObject();
            }
        }
    }
}
