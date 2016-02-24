using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Io.Network;
using AngleSharp.Services.Default;
using Knapcode.ConnectorRide.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Knapcode.ConnectorRide.Core
{
    public class ConnectorClient
    {
        private static readonly Regex StopTimeRegex = new Regex(
            @"(?<Hour>\d+):(?<Minute>\d+) (?<Period>AM|PM)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

            // extract the name
            var name = ExtractName(reference, scheduleDocument);
            var table = ExtractTable(reference, scheduleDocument);
            var mapReference = ExtractMapReference(reference, scheduleDocument);

            return new Schedule
            {
                Name = name,
                Table = table,
                MapReference = mapReference
            };
        }

        public async Task<Map> GetMapAsync(MapReference reference)
        {
            // get the map page
            var mapDocument = await _lazyContext.Value.OpenAsync(reference.Href).ConfigureAwait(false);

            // extract the stops
            var mapScript = mapDocument
                .QuerySelectorAll("script")
                .OfType<IHtmlScriptElement>()
                .Select(x => new { Element = x, Match = StopsRegex.Match(x.Text) })
                .FirstOrDefault(x => x.Match.Success);

            if (mapScript == null)
            {
                throw new ConnectorClientException($"The map data could not be found on the schedule map page: {reference.Href}");
            }

            // extract the stops
            var stops = ExtractMapStops(mapScript.Element.Text, mapScript.Match.Groups["Start"].Index);

            return new Map
            {
                Stops = stops
            };
        }

        private static string ExtractName(ScheduleReference reference, IDocument scheduleDocument)
        {
            var nameElement = scheduleDocument.QuerySelector("h2.pageTitle");
            if (nameElement == null)
            {
                throw new ConnectorClientException($"The schedule name could not be found on the schedule page: {reference.Href}");
            }

            var name = nameElement.TextContent.Trim();
            return name;
        }

        private static Table ExtractTable(ScheduleReference reference, IDocument scheduleDocument)
        {
            var scheduleTable = scheduleDocument
                .QuerySelectorAll("h2+div table")
                .OfType<IHtmlTableElement>()
                .FirstOrDefault();
            if (scheduleTable == null)
            {
                throw new ConnectorClientException($"The schedule table could not be found on the schedule page: {reference.Href}");
            }

            var stops = scheduleTable
                .QuerySelectorAll("th")
                .Select(headerCell => new TableStop
                {
                    Name = headerCell.TextContent.Trim(),
                    IsPick = headerCell.ClassList.Contains("ispick"),
                    IsHub = headerCell.ClassList.Contains("ishub")
                })
                .ToList();

            var rows = scheduleTable
                .QuerySelectorAll("tbody tr")
                .ToArray();
            var trips = new List<TableTrip>();
            foreach (var row in rows)
            {
                var cells = row
                    .QuerySelectorAll("td")
                    .OfType<IHtmlTableDataCellElement>()
                    .ToArray();

                if (cells.Length != stops.Count)
                {
                    throw new ConnectorClientException($"One of rows of the schedule table does not have the right number of columns on the schedule page: {reference.Href}");
                }

                var stopTimes = new List<TableStopTime>();
                for (int i = 0; i < cells.Length; i++)
                {
                    var cellText = cells[i].TextContent.Trim();
                    if (cellText == "----")
                    {
                        continue;
                    }

                    var stopTimeMatch = StopTimeRegex.Match(cellText);
                    int hour = int.Parse(stopTimeMatch.Groups["Hour"].Value);
                    int minute = int.Parse(stopTimeMatch.Groups["Minute"].Value);
                    string period = stopTimeMatch.Groups["Period"].Value.ToUpper();

                    stopTimes.Add(new TableStopTime
                    {
                        StopName = stops[i].Name,
                        Hour = period == "PM" ? hour + 12 : hour,
                        Minute = minute
                    });
                }

                trips.Add(new TableTrip
                {
                    StopTimes = stopTimes.ToArray()
                });
            }

            return new Table
            {
                Stops = stops.ToArray(),
                Trips = trips.ToArray()
            };
        }

        private MapReference ExtractMapReference(ScheduleReference reference, IDocument scheduleDocument)
        {
            var mapLink = scheduleDocument
                .QuerySelectorAll("a[href]")
                .OfType<IHtmlAnchorElement>()
                .FirstOrDefault(a => a.Href.Contains("Schedules/Map?name="));
            if (mapLink == null)
            {
                throw new ConnectorClientException($"The map link could not be found on the schedule page: {reference.Href}");
            }

            return new MapReference {Href = mapLink.Href};
        }

        private MapStop[] ExtractMapStops(string input, int offset)
        {
            var textReader = new StringReader(input.Substring(offset));
            var jsonReader = new JsonTextReader(textReader);
            var token = JToken.ReadFrom(jsonReader);

            var stops = token.ToObject<MapStop[]>();

            return stops;
        }

        private IBrowsingContext GetContext()
        {
            var requester = new HttpClientRequester(_client);
            var loaderService = new LoaderService(new[] { requester });
            var configuration = Configuration.Default.With(loaderService);

            return BrowsingContext.New(configuration);
        }
    }
}
