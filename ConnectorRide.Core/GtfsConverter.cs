using System.Collections.Generic;
using System.Linq;
using Knapcode.ConnectorRide.Common.Models;
using Knapcode.ConnectorRide.Core.GtfsModels;
using Schedule = Knapcode.ConnectorRide.Common.Models.Schedule;

namespace Knapcode.ConnectorRide.Core
{
    public interface IGtfsConverter
    {
        GtfsFeed ConvertToFeed(ScrapeResult scrapeResult, bool groupAmPm);
    }

    public class GtfsConverter : IGtfsConverter
    {
        private static readonly IReadOnlyDictionary<string, string> ScheduleNameMapping = new Dictionary<string, string>
        {
            { "BALLARD-WHITTIER HEIGHTS PM", "BALLARD-WHITTIER PM" },
        };

        public GtfsFeed ConvertToFeed(ScrapeResult scrapeResult, bool groupAmPm)
        {
            var context = new ConversionContext { ScrapeResult = scrapeResult, GroupAmPm = groupAmPm };

            // The scraped models are basically a subset of GTFS. The main difference is that 
            // the AM and PM trips in the scraped models are separate routes (and are called
            // "schedules"). If "groupAmPm" true, then combine schedules that have the except for
            // "AM" or "PM" suffix.
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
                Agencies = new List<Agency> { context.Agency },
                Stops = context.StopNames.Select(x => context.StopAndClientData[x].Stop).ToList(),
                Routes = context.RouteNames.Select(x => context.Routes[x]).ToList(),
                Trips = context.Trips.ToList(),
                StopTimes = context.StopTimes.ToList(),
                Calendar = new List<Service> { context.Service },
                Shapes = context.ShapePoints.ToList()
            };
        }

        private void ConvertTripsThenStopTimes(ConversionContext context)
        {
            context.Trips = new List<Trip>();
            context.StopTimes = new List<StopTime>();

            foreach (var pair in context.SchedulePairs)
            {
                if (pair.Am != null)
                {
                    ConvertTripThenStopTimes(context, pair.Name, pair.Am, Period.Am);
                }

                if (pair.Pm != null)
                {
                    ConvertTripThenStopTimes(context, pair.Name, pair.Pm, Period.Pm);
                }
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
                    var stopAndClientData = context.StopAndClientData[tableStopTime.StopName];
                    var time = new Time(tableStopTime.Hour, tableStopTime.Minute, 0);

                    PickupType pickupType;
                    DropOffType dropOffType;

                    if ((period == Period.Am && stopAndClientData.IsHub) ||
                        (period == Period.Pm && !stopAndClientData.IsHub))
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
                        StopId = stopAndClientData.Stop.Id,
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
            context.Routes = new Dictionary<string, Route>();
            context.RouteNames = new List<string>();

            foreach (var pair in context.SchedulePairs)
            {
                var route = new Route
                {
                    Id = context.Routes.Count.ToString(),
                    LongName = pair.Name,
                    Type = RouteType.Bus
                };

                context.Routes.Add(pair.Name, route);
                context.RouteNames.Add(pair.Name);
            }
        }

        private void ConvertStops(ConversionContext context)
        {
            context.StopAndClientData = new Dictionary<string, StopAndClientData>();
            context.StopNames = new List<string>();

            foreach (var pair in context.SchedulePairs)
            {
                if (pair.Am != null)
                {
                    ConvertStops(context, pair.Am);
                }

                if (pair.Pm != null)
                {
                    ConvertStops(context, pair.Pm);
                }
            }
        }

        private void ConvertStops(ConversionContext context, Schedule schedule)
        {
            foreach (var tableStop in schedule.Table.Stops)
            {
                StopAndClientData stopAndClientData;
                if (!context.StopAndClientData.TryGetValue(tableStop.Name, out stopAndClientData))
                {
                    stopAndClientData = new StopAndClientData
                    {
                        Stop = new Stop { Name = tableStop.Name }
                    };
                    context.StopAndClientData[tableStop.Name] = stopAndClientData;
                    context.StopNames.Add(tableStop.Name);
                }
                
                stopAndClientData.IsHub = tableStop.IsHub;
            }

            foreach (var mapStop in schedule.Map.Stops)
            {
                StopAndClientData stopAndClientData;
                if (!context.StopAndClientData.TryGetValue(mapStop.Name, out stopAndClientData))
                {
                    stopAndClientData = new StopAndClientData
                    {
                        Stop = new Stop { Name = mapStop.Name }
                    };
                    context.StopAndClientData[mapStop.Name] = stopAndClientData;
                    context.StopNames.Add(mapStop.Name);
                }
                
                stopAndClientData.IsHub = mapStop.IsHub;
                stopAndClientData.Stop.Id = mapStop.Id.ToString();
                stopAndClientData.Stop.Desc = string.Join(", ", new[] { mapStop.Address, mapStop.City, mapStop.ZipCode }.Where(x => x != null));
                stopAndClientData.Stop.Lat = mapStop.Latitude;
                stopAndClientData.Stop.Lon = mapStop.Longitude;
            }
        }

        private void ConvertShapes(ConversionContext context)
        {
            context.ShapeIds = new Dictionary<Schedule, string>();
            context.ShapePoints = new List<ShapePoint>();

            foreach (var pair in context.SchedulePairs)
            {
                if (pair.Am != null)
                {
                    ConvertShapes(context, pair.Am);
                }

                if (pair.Pm != null)
                {
                    ConvertShapes(context, pair.Pm);
                }
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
            context.SchedulePairs = new List<SchedulePair>();

            var nameGroups = context.ScrapeResult
                .Schedules
                .Select(s => new { NameWithPeriod = GetNameWithPeriod(s.Name), Schedule = s })
                .GroupBy(s => s.NameWithPeriod.Name);

            foreach (var nameGroup in nameGroups)
            {
                string name = nameGroup.Key;
                var schedules = nameGroup.ToList();

                var periodGroups = schedules.ToLookup(p => p.NameWithPeriod.Period);
                var am = periodGroups[Period.Am].ToList();
                if (am.Count != 1)
                {
                    throw new ConnectorRideException($"There should be exactly one (not {am.Count}) AM schedules with the name '{name}'.");
                }

                var pm = periodGroups[Period.Pm].ToList();
                if (pm.Count != 1)
                {
                    throw new ConnectorRideException($"There should be exactly one (not {pm.Count}) PM schedules with the name '{name}'.");
                }

                if (context.GroupAmPm)
                {
                    context.SchedulePairs.Add(new SchedulePair
                    {
                        Name = name,
                        Am = am[0].Schedule,
                        Pm = pm[0].Schedule
                    });
                }
                else
                {
                    context.SchedulePairs.Add(new SchedulePair
                    {
                        Name = am[0].Schedule.Name,
                        Am = am[0].Schedule
                    });

                    context.SchedulePairs.Add(new SchedulePair
                    {
                        Name = pm[0].Schedule.Name,
                        Pm = pm[0].Schedule
                    });
                }
            }
        }

        private NameWithPeriod GetNameWithPeriod(string name)
        {
            string mappedName;
            if (ScheduleNameMapping.TryGetValue(name, out mappedName))
            {
                name = mappedName;
            }

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
            public bool GroupAmPm { get; set; }
            public List<SchedulePair> SchedulePairs { get; set; }
            public Agency Agency { get; set; }
            public Service Service { get; set; }
            public Dictionary<Schedule, string> ShapeIds { get; set; }
            public List<ShapePoint> ShapePoints { get; set; }
            public Dictionary<string, StopAndClientData> StopAndClientData { get; set; }
            public List<string> StopNames { get; set; }
            public Dictionary<string, Route> Routes { get; set; } 
            public List<string> RouteNames { get; set; } 
            public List<Trip> Trips { get; set; }
            public List<StopTime> StopTimes { get; set; }
        }

        public class StopAndClientData
        {
            public Stop Stop { get; set; }
            public bool IsHub { get; set; }
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
