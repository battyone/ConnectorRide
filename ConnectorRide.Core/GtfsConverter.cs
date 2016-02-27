using System.Collections.Generic;
using System.Linq;
using Knapcode.ConnectorRide.Core.ClientModels;
using Knapcode.ConnectorRide.Core.GtfsModels;
using Knapcode.ConnectorRide.Core.ScraperModels;
using Schedule = Knapcode.ConnectorRide.Core.ScraperModels.Schedule;

namespace Knapcode.ConnectorRide.Core
{
    public class GtfsConverter
    {
        public GtfsFeed ConvertToFeed(ScrapeResult scrapeResult)
        {
            var context = new ConversionContext { ScrapeResult = scrapeResult };

            // The scraped models are close to being a subset of GTFS. The main difference is that 
            // the AM and PM trips in the scraped models are separate routes (and are called
            // "schedules"). First step, then, is to associate AM and PM schedules.
            InitializeSchedulePairs(context);

            // Initialize the agency and calendar based on known constants.
            InitializeAgency(context);
            InitializeCalendar(context);

            // Convert the scrape models to GTFS models.
            ConvertStops(context);
            ConvertRoutes(context);
            ConvertShapes(context);
            ConvertTripsThenStopTimes(context);

            // Collect the output data.
            return new GtfsFeed
            {
                Agencies = new[] { context.Agency },
                Stops = context.StopAndTableStops.Values.Select(p => p.Stop).ToArray(),
                Routes = context.Routes.Values.ToArray(),
                Trips = context.Trips.ToArray(),
                StopTimes = context.StopTimes.ToArray(),
                Calendar = new[] { context.Service },
                Shapes = context.ShapePoints.ToArray()
            };
        }

        private void ConvertTripsThenStopTimes(ConversionContext context)
        {
            context.Trips = new List<Trip>();
            context.StopTimes = new List<StopTime>();

            foreach (var pair in context.SchedulePairs)
            {
                ConvertTripThenStopTimes(context, pair.Name, pair.Am, Period.Am);
                ConvertTripThenStopTimes(context, pair.Name, pair.Pm, Period.Pm);
            }
        }

        private void ConvertTripThenStopTimes(ConversionContext context, string name, Schedule schedule, Period period)
        {
            foreach (var tableTrip in schedule.Table.Trips)
            {
                var tripId = context.Trips.Count.ToString();

                // add the trip
                context.Trips.Add(new Trip
                {
                    RouteId = context.Routes[name].Id,
                    ServiceId = context.Service.Id,
                    Id = tripId,
                    DirectionId = period == Period.Pm,
                    ShapeId = context.ShapeIds[schedule]
                });

                // add the trip's stop times
                uint stopSequence = 0;
                foreach (var tableStopTime in tableTrip.StopTimes)
                {
                    var stopAndTableStop = context.StopAndTableStops[tableStopTime.StopName];
                    var time = new Time(tableStopTime.Hour, tableStopTime.Minute, 0);

                    PickupType pickupType;
                    DropOffType dropOffType;

                    if ((period == Period.Am && stopAndTableStop.TableStop.IsHub) ||
                        (period == Period.Pm && !stopAndTableStop.TableStop.IsHub))
                    {
                        pickupType = PickupType.NoPickupAvailable;
                        dropOffType = DropOffType.RegularlyScheduled;
                    }
                    else
                    {
                        pickupType = PickupType.RegularlyScheduled;
                        dropOffType = DropOffType.NoDropOffAvailable;
                    }

                    context.StopTimes.Add(new StopTime
                    {
                        TripId = tripId,
                        ArrivalTime = time,
                        DepartureTime = time,
                        StopId = stopAndTableStop.Stop.Id,
                        Sequence = stopSequence,
                        PickupType = pickupType,
                        DropOffType = dropOffType
                    });

                    stopSequence++;
                }
            }
        }

        private void InitializeAgency(ConversionContext context)
        {
            context.Agency = new Agency
            {
                Name = "Microsoft Connector",
                Url = "https://www.connectorride.mobi/",
                Phone = "425-502-5035",
                Timezone = "America/Los_Angeles"
            };
        }

        private void InitializeCalendar(ConversionContext context)
        {
            context.Service = new Service
            {
                Id = "0",
                Monday = true,
                Tuesday = true,
                Wednesday = true,
                Thursday = true,
                Friday = true,
                Saturday = false,
                Sunday = false,
                StartDate = new Date(2000, 1, 1),
                EndDate = new Date(2020, 1, 1)
            };
        }

        private void ConvertRoutes(ConversionContext context)
        {
            var routes = new Dictionary<string, Route>();

            foreach (var pair in context.SchedulePairs)
            {
                var route = new Route
                {
                    Id = routes.Count.ToString(),
                    LongName = pair.Name,
                    Type = RouteType.Bus
                };

                routes.Add(pair.Name, route);
            }

            context.Routes = routes;
        }

        private void ConvertStops(ConversionContext context)
        {
            var stopAndTableStops = new Dictionary<string, StopAndTableStop>();
            foreach (var pair in context.SchedulePairs)
            {
                ConvertStops(stopAndTableStops, pair.Am);
                ConvertStops(stopAndTableStops, pair.Pm);
            }

            context.StopAndTableStops = stopAndTableStops;
        }

        private void ConvertStops(Dictionary<string, StopAndTableStop> stops, Schedule schedule)
        {
            foreach (var tableStop in schedule.Table.Stops)
            {
                StopAndTableStop stopAndTableStop;
                if (!stops.TryGetValue(tableStop.Name, out stopAndTableStop))
                {
                    stops[tableStop.Name] = new StopAndTableStop
                    {
                        Stop = new Stop
                        {
                            Name = tableStop.Name
                        },
                        TableStop = tableStop
                    };
                }
            }

            foreach (var mapStop in schedule.Map.Stops)
            {
                var stopAndTableStop = stops[mapStop.Name];

                stopAndTableStop.Stop.Id = mapStop.Id.ToString();
                stopAndTableStop.Stop.Desc = string.Join(", ", new[] {mapStop.Address, mapStop.City, mapStop.ZipCode}.Where(x => x != null));
                stopAndTableStop.Stop.Lat = mapStop.Latitude;
                stopAndTableStop.Stop.Lon = mapStop.Longitude;
            }
        }

        private void ConvertShapes(ConversionContext context)
        {
            context.ShapeIds = new Dictionary<Schedule, string>();
            context.ShapePoints = new List<ShapePoint>();

            foreach (var pair in context.SchedulePairs)
            {
                ConvertShapes(context, pair.Am);
                ConvertShapes(context, pair.Pm);
            }
        }

        private void ConvertShapes(ConversionContext context, Schedule schedule)
        {
            string shapeId = context.ShapeIds.Count.ToString();
            context.ShapeIds.Add(schedule, shapeId);

            uint sequence = 0;
            foreach (var location in schedule.Map.Polyline.MapWayPoints)
            {
                context.ShapePoints.Add(new ShapePoint
                {
                    ShapeId = shapeId,
                    Lat = location.Latitude,
                    Lon = location.Longitude,
                    Sequence = sequence
                });

                sequence++;
            }
        }

        private void InitializeSchedulePairs(ConversionContext context)
        {
            var nameGroups = context.ScrapeResult
                .Schedules
                .Select(s => new {NameWithPeriod = GetNameWithPeriod(s.Name), Schedule = s})
                .GroupBy(s => s.NameWithPeriod.Name);

            var schedulePairs = new List<SchedulePair>();
            foreach (var nameGroup in nameGroups)
            {
                string name = nameGroup.Key;
                var schedules = nameGroup.ToArray();

                var periodGroups = schedules.ToLookup(p => p.NameWithPeriod.Period);
                var am = periodGroups[Period.Am].ToArray();
                if (am.Length != 1)
                {
                    throw new ConnectorRideException($"There should be exactly one (not {am.Length}) AM schedules with the name '{name}'.");
                }

                var pm = periodGroups[Period.Pm].ToArray();
                if (pm.Length != 1)
                {
                    throw new ConnectorRideException($"There should be exactly one (not {am.Length}) PM schedules with the name '{name}'.");
                }

                schedulePairs.Add(new SchedulePair
                {
                    Name = name,
                    Am = am[0].Schedule,
                    Pm = pm[0].Schedule
                });
            }

            context.SchedulePairs = schedulePairs;
        }

        private NameWithPeriod GetNameWithPeriod(string name)
        {
            if (name.EndsWith(" AM"))
            {
                return new NameWithPeriod {Name = name.Substring(0, name.Length - 3), Period = Period.Am};
            }

            if (name.EndsWith(" PM"))
            {
                return new NameWithPeriod {Name = name.Substring(0, name.Length - 3), Period = Period.Pm};
            }

            throw new ConnectorRideException($"The name schedule name '{name}' should end with ' AM' or ' PM'.");
        }

        private class ConversionContext
        {
            public ScrapeResult ScrapeResult { get; set; }
            public List<SchedulePair> SchedulePairs { get; set; }
            public Agency Agency { get; set; }
            public Service Service { get; set; }
            public Dictionary<Schedule, string> ShapeIds { get; set; }
            public List<ShapePoint> ShapePoints { get; set; }
            public Dictionary<string, StopAndTableStop> StopAndTableStops { get; set; }
            public Dictionary<string, Route> Routes { get; set; } 
            public List<Trip> Trips { get; set; }
            public List<StopTime> StopTimes { get; set; }
        }

        public class StopAndTableStop
        {
            public Stop Stop { get; set; }
            public TableStop TableStop { get; set; }
        }

        private class NameWithPeriod
        {
            public string Name { get; set; }
            public Period Period { get; set; }
        }

        private enum Period { Am, Pm }

        private class SchedulePair
        {
            public string Name { get; set; }
            public Schedule Am { get; set; }
            public Schedule Pm { get; set; }
        }
    }
}
