using System;
using System.Net.Http;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core;
using Knapcode.ConnectorRide.Core.Abstractions;
using Newtonsoft.Json;

namespace Knapcode.ConnectorRide.Tool
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        private static async Task MainAsync(string[] args)
        {
            // initialize
            var systemTime = new SystemTime();
            var httpClient = new HttpClient();
            var connectorClient = new Client(httpClient);
            var connectorScraper = new Scraper(systemTime, connectorClient);
            var jsonWriter = new JsonTextWriter(Console.Out);
            var writer = new JsonScrapeResultWriter(jsonWriter);

            // scrape
            await connectorScraper.RealTimeScrapeAsync(writer).ConfigureAwait(false);
            Console.WriteLine();
        }
    }
}
