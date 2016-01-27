using System;
using System.Net.Http;
using System.Threading.Tasks;
using Knapcode.SocketToMe.Http;

namespace Knapcode.ConnectorRide.Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            // initialize
            var handler = new NetworkHandler();
            var httpClient = new HttpClient(handler);
            var connectorClient = new ConnectorClient(httpClient);
            var connectorScraper = new ConnectorScraper(connectorClient);

            // scrape
            await connectorScraper.ScrapeSchedulesAsync(Console.Out);
            Console.WriteLine();
        }
    }
}
