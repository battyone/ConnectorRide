using System.Collections.Generic;

namespace Knapcode.ConnectorRide.Core.Models
{
    public class Schedule
    {
        public string Name { get; set; }
        public MapStop[] MapStops { get; set; }
        public TableTrip[] TableTrips { get; set; }
    }
}