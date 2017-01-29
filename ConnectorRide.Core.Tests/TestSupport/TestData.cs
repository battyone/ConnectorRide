using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Common.Models;
using Knapcode.ConnectorRide.Core.GtfsModels;

namespace Knapcode.ConnectorRide.Core.Tests.TestSupport
{
    public static class TestData
    {
        public static Stream GetScrapeResultStream()
        {
            return GetResourceStream("Knapcode.ConnectorRide.Core.Tests.TestData.ScrapeResult.json");
        }

        public static async Task<ScrapeResult> GetScrapeResultAsync()
        {
            using (var stream = GetScrapeResultStream())
            {
                return await new ScrapeResultSerializer().DeserializeAsync(stream, false);
            }
        }

        public static async Task<GtfsFeed> GetGtfsFeedAsync(bool groupAmPm)
        {
            var scrapeResult = await GetScrapeResultAsync();
            return new GtfsConverter().ConvertToFeed(scrapeResult, groupAmPm);
        }

        private static Stream GetResourceStream(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream(name);
        }
    }
}
