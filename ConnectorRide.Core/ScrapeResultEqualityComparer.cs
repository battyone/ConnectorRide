using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ToStorage.Core;
using Newtonsoft.Json;

namespace Knapcode.ConnectorRide.Core
{
    public class ScrapeResultEqualityComparer : IAsyncEqualityComparer<Stream>
    {
        private readonly IScrapeResultSerializer _serializer;

        public ScrapeResultEqualityComparer(IScrapeResultSerializer serializer)
        {
            _serializer = serializer;
        }
        
        public async Task<bool> EqualsAsync(Stream x, Stream y, CancellationToken cancellationToken)
        {
            var oldResult = await _serializer.DeserializeAsync(x, true);
            var newResult = await _serializer.DeserializeAsync(y, true);

            var oldJson = JsonConvert.SerializeObject(oldResult.Schedules);
            var newJson = JsonConvert.SerializeObject(newResult.Schedules);

            return oldJson == newJson;
        }
    }
}