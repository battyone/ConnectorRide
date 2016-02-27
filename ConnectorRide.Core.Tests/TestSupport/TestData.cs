using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core.GtfsModels;
using Knapcode.ConnectorRide.Core.ScraperModels;

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
                return await new ScrapeResultSerializer().DeserializeAsync(stream);
            }
        }

        public static async Task<GtfsFeed> GetGtfsFeedAsync()
        {
            var scrapeResult = await GetScrapeResultAsync();
            return new GtfsConverter().ConvertToFeed(scrapeResult);
        }

        private static Stream GetResourceStream(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream(name);
        }
    }
}
