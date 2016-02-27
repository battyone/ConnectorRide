using System.IO;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Newtonsoft.Json;

namespace Knapcode.ConnectorRide.Core
{
    public interface ISerializer
    {
        Task<ScrapeResult> DeserializeScrapeResultAsync(Stream stream);
    }

    public class Serializer : ISerializer
    {
        public Task<ScrapeResult> DeserializeScrapeResultAsync(Stream stream)
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