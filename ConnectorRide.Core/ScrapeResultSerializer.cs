using System.IO;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Newtonsoft.Json;

namespace Knapcode.ConnectorRide.Core
{
    public interface IScrapeResultSerializer
    {
        Task<ScrapeResult> DeserializeAsync(Stream stream);
    }

    public class ScrapeResultSerializer : IScrapeResultSerializer
    {
        public Task<ScrapeResult> DeserializeAsync(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var jsonSerializer = new JsonSerializer();
                var scrapeResult = jsonSerializer.Deserialize<ScrapeResult>(jsonReader);
                return Task.FromResult(scrapeResult);
            }
        }
    }
}