using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Knapcode.ToStorage.Core;

namespace Knapcode.ConnectorRide.Core
{
    public class ZipArchiveEqualityComparer : IAsyncEqualityComparer<Stream>
    {
        public async Task<bool> EqualsAsync(Stream x, Stream y, CancellationToken cancellationToken)
        {
            var streamComparer = new OrdinalStreamEqualityComparer();

            using (var zipX = new ZipArchive(x, ZipArchiveMode.Read, true))
            using (var zipY = new ZipArchive(y, ZipArchiveMode.Read, true))
            {
                var entriesX = new HashSet<string>(zipX.Entries.Select(e => e.FullName));
                var entriesY = new HashSet<string>(zipY.Entries.Select(e => e.FullName));
                if (!entriesX.SetEquals(entriesY))
                {
                    return false;
                }

                foreach (var entry in entriesX)
                {
                    using (var entryX = zipX.GetEntry(entry).Open())
                    using (var entryY = zipY.GetEntry(entry).Open())
                    {
                        if (!await streamComparer.EqualsAsync(entryX, entryY, CancellationToken.None))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
