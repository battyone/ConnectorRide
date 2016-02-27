namespace Knapcode.ConnectorRide.Core.GtfsModels
{
    public class StopTime
    {
        public string TripId { get; set; }
        public Time ArrivalTime { get; set; }
        public Time DepartureTime { get; set; }
        public string StopId { get; set; }
        public uint Sequence { get; set; }
        // public string Headsign { get; set; }
        public PickupType? PickupType { get; set; }
        public DropOffType? DropOffType { get; set; }
        // public double? ShapeDistTraveled { get; set; }
        // public Timepoint? Timepoint { get; set; }
    }
}