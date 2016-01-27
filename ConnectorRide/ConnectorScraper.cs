using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Knapcode.ConnectorRide
{
    public class ConnectorScraper
    {
        private readonly ConnectorClient _client;

        public ConnectorScraper(ConnectorClient client)
        {
            _client = client;
        }

        public async Task ScrapeSchedulesAsync(TextWriter textWriter)
        {
            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                jsonWriter.IndentChar = ' ';
                jsonWriter.Indentation = 4;
                jsonWriter.Formatting = Formatting.Indented;

                var scheduleReferences = await _client.GetSchedulesAsync();

                bool started = false;
                foreach (var scheduleReference in scheduleReferences)
                {
                    var schedule = await _client.GetScheduleAsync(scheduleReference);

                    if (!started)
                    {
                        jsonWriter.WriteStartArray();
                        started = true;
                    }

                    var json = JObject.FromObject(schedule);
                    json.WriteTo(jsonWriter);
                }

                jsonWriter.WriteEndArray();
            }
        }
    }
}
