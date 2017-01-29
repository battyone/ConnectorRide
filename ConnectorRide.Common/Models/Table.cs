using System.Collections.Generic;

namespace Knapcode.ConnectorRide.Common.Models
{
    public class Table
    {
        public IEnumerable<TableTrip> Trips { get; set; }
        public IEnumerable<TableStop> Stops { get; set; }
    }
}
