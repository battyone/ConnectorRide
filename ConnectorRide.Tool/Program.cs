using System;
using System.Net.Http;
using System.Threading.Tasks;
using Knapcode.ConnectorRide.Core;
using Knapcode.SocketToMe.Http;

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
            var handler = new NetworkHandler();
            var httpClient = new HttpClient(handler);
            var connectorClient = new ConnectorClient(httpClient);
            var connectorScraper = new ConnectorScraper(connectorClient);

            // scrape
            await connectorScraper.ScrapeSchedulesAsync(Console.Out).ConfigureAwait(false);
            Console.WriteLine();
        }
    }
}
