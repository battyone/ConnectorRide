namespace Knapcode.ConnectorRide.Core.GtfsModels
{
    public class GtfsFeed
    {
        public Agency[] Agencies { get; set; }
        public Stop[] Stops { get; set; }
        public Route[] Routes { get; set; }
        public Trip[] Trips { get; set; }
        public StopTime[] StopTimes { get; set; }
        public Service[] Calendar { get; set; }
        public ShapePoint[] Shapes { get; set; }
    }
}