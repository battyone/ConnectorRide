using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ToStorage.Core;
using Newtonsoft.Json;

namespace Knapcode.ConnectorRide.Core
{
    public class ScrapeResultCollapserComparer : ICollapserComparer
    {
        private readonly IScrapeResultSerializer _serializer;

        public ScrapeResultCollapserComparer(IScrapeResultSerializer serializer)
        {
            _serializer = serializer;
        }

        public int Compare(string x, string y)
        {
            return StringComparer.Ordinal.Compare(x, y);
        }

        public async Task<bool> EqualsAsync(string nameX, Stream streamX, string nameY, Stream streamY, CancellationToken cancellationToken)
        {
            var oldResult = await _serializer.DeserializeAsync(streamX, true);
            var newResult = await _serializer.DeserializeAsync(streamY, true);

            var oldJson = JsonConvert.SerializeObject(oldResult.Schedules);
            var newJson = JsonConvert.SerializeObject(newResult.Schedules);

            return oldJson == newJson;
        }
    }
}