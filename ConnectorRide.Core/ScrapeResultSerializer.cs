using System.IO;
using System.Text;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Common.Models;
using Newtonsoft.Json;

namespace Knapcode.ConnectorRide.Core
{
    public interface IScrapeResultSerializer
    {
        Task<ScrapeResult> DeserializeAsync(Stream stream, bool leaveOpen);
    }

    public class ScrapeResultSerializer : IScrapeResultSerializer
    {
        public Task<ScrapeResult> DeserializeAsync(Stream stream, bool leaveOpen)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, false, 4092, leaveOpen))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var jsonSerializer = new JsonSerializer();
                var scrapeResult = jsonSerializer.Deserialize<ScrapeResult>(jsonReader);
                return Task.FromResult(scrapeResult);
            }
        }
    }
}