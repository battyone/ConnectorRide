using System.Collections.Generic;

namespace Knapcode.ConnectorRide.Core
{
    public class Schedule
    {
        public string Name { get; set; }
        public IEnumerable<Stop> Stops { get; set; }
    }
}