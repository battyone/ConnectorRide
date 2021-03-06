﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Io.Network;
using AngleSharp.Network;
using AngleSharp.Services.Default;
using Knapcode.ConnectorRide.Common.Models;
using Knapcode.ConnectorRide.Core.ClientModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Schedule = Knapcode.ConnectorRide.Core.ClientModels.Schedule;

namespace Knapcode.ConnectorRide.Core
{
    public interface IClient
    {
        Task<IEnumerable<ScheduleReference>> GetScheduleReferencesAsync();
        Task<Schedule> GetScheduleAsync(ScheduleReference reference);
        Task<Map> GetMapAsync(MapReference reference);
        string Version { get; }
    }

    public class Client : IClient
    {
        private static readonly Regex StopTimeRegex = new Regex(
            @"(?<Hour>\d+):(?<Minute>\d+) (?<Period>AM|PM)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PolylineRegex = new Regex(
            @"[^\w]+mapline\s*=\s*(?<Start>\{)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex StopsRegex = new Regex(
            @"[^\w]+stops\s*=\s*(?<Start>\[)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Lazy<IBrowsingContext> _lazyContext;
        private readonly HttpClient _client;

        public Client(HttpClient client)
        {
            _client = client;
            _lazyContext = new Lazy<IBrowsingContext>(GetContext);
        }

        public async Task<IEnumerable<ScheduleReference>> GetScheduleReferencesAsync()
        {
            var document = await GetDocumentAsync("https://www.connectorride.mobi/Schedules");

            return document
                .QuerySelectorAll("a[href]")
                .OfType<IHtmlAnchorElement>()
                .Where(a => a.Href.Contains("Schedules/Schedule2?name="))
                .Select(a => new ScheduleReference { Href = a.Href })
                .ToList();
        }

        public async Task<Schedule> GetScheduleAsync(ScheduleReference reference)
        {
            // get the schedule page
            var scheduleDocument = await GetDocumentAsync(reference.Href);

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
            var mapDocument = await GetDocumentAsync(reference.Href);

            // extract the polyline
            var polylineScript = mapDocument
                .QuerySelectorAll("script")
                .OfType<IHtmlScriptElement>()
                .Select(x => new { Element = x, Match = PolylineRegex.Match(x.Text) })
                .FirstOrDefault(x => x.Match.Success);

            if (polylineScript == null)
            {
                throw new ConnectorRideException($"The polyline data could not be found on the schedule map page: {reference.Href}");
            }
            
            var polyline = ExtractObject<Polyline>(polylineScript.Element.Text, polylineScript.Match.Groups["Start"].Index);

            // extract the stops
            var stopsScript = mapDocument
                .QuerySelectorAll("script")
                .OfType<IHtmlScriptElement>()
                .Select(x => new { Element = x, Match = StopsRegex.Match(x.Text) })
                .FirstOrDefault(x => x.Match.Success);

            if (stopsScript == null)
            {
                throw new ConnectorRideException($"The stop data could not be found on the schedule map page: {reference.Href}");
            }
            
            var stops = ExtractObject<MapStop[]>(stopsScript.Element.Text, stopsScript.Match.Groups["Start"].Index);

            return new Map
            {
                Stops = stops,
                Polyline = polyline
            };
        }

        public string Version { get; } = "3.2.0";

        private static string ExtractName(ScheduleReference reference, IDocument scheduleDocument)
        {
            var nameElement = scheduleDocument.QuerySelector("h2.pageTitle");
            if (nameElement == null)
            {
                throw new ConnectorRideException($"The schedule name could not be found on the schedule page: {reference.Href}");
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
                throw new ConnectorRideException($"The schedule table could not be found on the schedule page: {reference.Href}");
            }

            var stops = scheduleTable
                .QuerySelectorAll("th")
                .Where(headerCell => !headerCell.ClassList.Contains("IsHeading"))
                .Select(headerCell => new TableStop
                {
                    Name = headerCell.TextContent.Trim(),
                    IsPick = headerCell.ClassList.Contains("ispick"),
                    IsHub = headerCell.ClassList.Contains("ishub")
                })
                .ToList();

            var trips = new List<TableTrip>();

            var rows = scheduleTable
                   .Children
                   .FirstOrDefault(x => x.NodeName == "TBODY")?
                   .Children?
                   .Where(x => x.NodeName == "TR")?
                   .ToList();
            
            if (rows != null)
            {
                var rowCells = new List<List<IHtmlTableDataCellElement>>();

                foreach (var row in rows)
                {
                    var cells = row
                        .Children
                        .Where(x => x.NodeName == "TD")
                        .OfType<IHtmlTableDataCellElement>()
                        .Skip(1)
                        .ToList();

                    rowCells.Add(cells);
                }

                if (rowCells.Any()
                    && rowCells.All(x => x.Count == rowCells[0].Count)
                    && rowCells[0].Count < stops.Count)
                {
                    stops = stops.GetRange(0, rowCells[0].Count);
                }

                foreach (var cells in rowCells)
                {
                    if (cells.Count != stops.Count)
                    {
                        throw new ConnectorRideException($"One of rows of the schedule table does not have the right number of columns on the schedule page: {reference.Href}");
                    }

                    var stopTimes = new List<TableStopTime>();
                    for (int i = 0; i < cells.Count; i++)
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
                        StopTimes = stopTimes.ToList()
                    });
                }
            }

            return new Table
            {
                Stops = stops.ToList(),
                Trips = trips.ToList()
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
                throw new ConnectorRideException($"The map link could not be found on the schedule page: {reference.Href}");
            }

            return new MapReference {Href = mapLink.Href};
        }

        private T ExtractObject<T>(string javaScript, int offset)
        {
            var textReader = new StringReader(javaScript.Substring(offset));
            var jsonReader = new JsonTextReader(textReader);
            var token = JToken.ReadFrom(jsonReader);

            return token.ToObject<T>();
        }

        private IBrowsingContext GetContext()
        {
            var requester = new HttpClientRequester(_client);
            var loaderService = new LoaderService(new[] { requester });
            var configuration = Configuration.Default.With(loaderService);

            return BrowsingContext.New(configuration);
        }

        public async Task<IDocument> GetDocumentAsync(string url)
        {
            var request = DocumentRequest.Get(Url.Create(url));
            request.Headers["User-Agent"] = UserAgent;
            return await _lazyContext.Value.OpenAsync(request, CancellationToken.None);
        }

        private string UserAgent => "Mozilla/5.0 " +
                                    "(compatible; " +
                                    $"ConnectorRide/{Version}; " +
                                    "+https://github.com/joelverhagen/ConnectorRide)";
    }
}
