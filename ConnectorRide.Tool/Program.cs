using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CommandLine;
using Knapcode.ConnectorRide.Core;
using Knapcode.ToStorage.Core;
using Knapcode.ToStorage.Core.Abstractions;
using Knapcode.ToStorage.Core.AzureBlobStorage;
using Newtonsoft.Json;
using Client = Knapcode.ConnectorRide.Core.Client;

namespace Knapcode.ConnectorRide.Tool
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return MainAsync(args).Result;
        }

        private static async Task<int> MainAsync(string[] args)
        {
            var result = Parser.Default.ParseArguments<ScrapeOptions, CollapseOptions>(args);
            if (result.Tag == ParserResultType.NotParsed)
            {
                return 1;
            }

            var parsedResult = (Parsed<object>) result;

            if (parsedResult.Value is ScrapeOptions)
            {
                // initialize
                var systemTime = new SystemTime();
                var httpClient = new HttpClient();
                var connectorClient = new Client(httpClient);
                var connectorScraper = new Scraper(systemTime, connectorClient);
                var jsonWriter = new JsonTextWriter(Console.Out);
                var writer = new JsonScrapeResultWriter(jsonWriter);

                // act
                await connectorScraper.RealTimeScrapeAsync(writer).ConfigureAwait(false);
                return 0;
            }

            if (parsedResult.Value is CollapseOptions)
            {
                // initialize
                var options = (CollapseOptions) parsedResult.Value;
                var pathBuilder = new PathBuilder();
                var collapser = new Collapser(pathBuilder);
                IAsyncEqualityComparer<Stream> comparer;
                switch (options.ComparisonType)
                {
                    case ComparisonType.ScrapeResult:
                        comparer = new ScrapeResultEqualityComparer();
                        break;

                    default:
                        comparer = new ZipArchiveEqualityComparer();
                        break;
                }

                var request = new CollapseRequest
                {
                    ConnectionString = options.ConnectionString,
                    PathFormat = options.PathFormat,
                    Container = options.Container,
                    Trace = Console.Out,
                    Comparer = new AdapterCollapserComparer(StringComparer.Ordinal, comparer)
                };

                // act
                await collapser.CollapseAsync(request);
                return 0;
            }

            return 0;
        }
    }
}
