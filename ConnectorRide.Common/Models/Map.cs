using System.Collections.Generic;

namespace Knapcode.ConnectorRide.Common.Models
{
    public class Map
    {
        public IEnumerable<MapStop> Stops { get; set; }
        public Polyline Polyline { get; set; }
    }
}
