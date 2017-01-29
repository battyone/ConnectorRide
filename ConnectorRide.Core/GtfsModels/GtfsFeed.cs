using System.Collections.Generic;

namespace Knapcode.ConnectorRide.Core.GtfsModels
{
    public class GtfsFeed
    {
        public List<Agency> Agencies { get; set; }
        public List<Stop> Stops { get; set; }
        public List<Route> Routes { get; set; }
        public List<Trip> Trips { get; set; }
        public List<StopTime> StopTimes { get; set; }
        public List<Service> Calendar { get; set; }
        public List<ShapePoint> Shapes { get; set; }
    }
}