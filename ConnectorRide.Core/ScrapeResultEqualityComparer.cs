using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ToStorage.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Knapcode.ConnectorRide.Core
{
    public class ScrapeResultEqualityComparer : IAsyncEqualityComparer<Stream>
    {
        public async Task<bool> EqualsAsync(Stream x, Stream y, CancellationToken cancellationToken)
        {
            await Task.Yield();

            using (var readerX = new StreamReader(x, Encoding.UTF8, false, 4096, true))
            using (var readerY = new StreamReader(y, Encoding.UTF8, false, 4096, true))
            using (var jsonReaderX = new JsonTextReader(readerX))
            using (var jsonReaderY = new JsonTextReader(readerY))
            {
                var jsonX = JToken.ReadFrom(jsonReaderX);
                var jsonY = JToken.ReadFrom(jsonReaderY);

                if (jsonX["Version"]?.ToString() == jsonY["Version"]?.ToString())
                {
                    return jsonX["Schedules"].ToString() == jsonY["Schedules"].ToString();
                }

                return false;
            }
        }
    }
}