using System.Collections.Generic;

namespace Knapcode.ConnectorRide.Common.Models
{
    public class TableTrip
    {
        public IEnumerable<TableStopTime> StopTimes { get; set; }
    }
}