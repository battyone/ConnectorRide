using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Io.Network;
using AngleSharp.Services.Default;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Knapcode.ConnectorRide.Core
{
    public class ConnectorClient
    {
        private static readonly Regex StopsRegex = new Regex(
            @"[^\w]+stops\s*=\s*(?<Start>\[)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Lazy<IBrowsingContext> _lazyContext;
        private readonly HttpClient _client;

        public ConnectorClient(HttpClient client)
        {
            _client = client;
            _lazyContext = new Lazy<IBrowsingContext>(GetContext);
        }

        public async Task<IEnumerable<ScheduleReference>> GetSchedulesAsync()
        {
            var document = await _lazyContext.Value.OpenAsync("https://www.connectorride.mobi/Schedules").ConfigureAwait(false);

            return document
                .QuerySelectorAll("a[href]")
                .OfType<IHtmlAnchorElement>()
                .Where(a => a.Href.Contains("Schedules/Schedule?name="))
                .Select(a => new ScheduleReference { Href = a.Href })
                .ToArray();
        }

        public async Task<Schedule> GetScheduleAsync(ScheduleReference reference)
        {
            // get the schedule page
            var scheduleDocument = await _lazyContext.Value.OpenAsync(reference.Href).ConfigureAwait(false);

            var mapLink = scheduleDocument
                .QuerySelectorAll("a[href]")
                .OfType<IHtmlAnchorElement>()
                .FirstOrDefault(a => a.Href.Contains("Schedules/Map?name="));

            if (mapLink == null)
            {
                throw new ConnectorClientException($"The map link could not be found on the schedule page: {reference.Href}");
            }

            // get the map page
            var mapDocument = await _lazyContext.Value.OpenAsync(mapLink.Href).ConfigureAwait(false);


            // extract the name
            var nameElement = mapDocument.QuerySelector("h2.pageTitle");
            if (nameElement == null)
            {
                throw new ConnectorClientException($"The schedule name could not be found on the schedule page: {reference.Href}.");
            }

            var name = nameElement.TextContent.Trim();
            
            // extract the stops
            var mapScript = mapDocument
                .QuerySelectorAll("script")
                .OfType<IHtmlScriptElement>()
                .Select(x => new { Element = x, Match = StopsRegex.Match(x.Text) })
                .FirstOrDefault(x => x.Match.Success);

            if (mapScript == null)
            {
                throw new ConnectorClientException($"The map data could not be found on the schedule map page: {mapLink.Href}");
            }


            // extract the stops
            var stops = ReadStops(mapScript.Element.Text, mapScript.Match.Groups["Start"].Index);

            return new Schedule
            {
                Name = name,
                Stops = stops
            };
        }

        private IBrowsingContext GetContext()
        {
            var requester = new HttpClientRequester(_client);
            var loaderService = new LoaderService(new[] { requester });
            var configuration = Configuration.Default.With(loaderService);

            return BrowsingContext.New(configuration);
        }

        private IEnumerable<Stop> ReadStops(string input, int offset)
        {
            var textReader = new StringReader(input.Substring(offset));
            var jsonReader = new JsonTextReader(textReader);
            var token = JToken.ReadFrom(jsonReader);

            var stops = token.ToObject<Stop[]>();

            return stops;
        }
    }
}
