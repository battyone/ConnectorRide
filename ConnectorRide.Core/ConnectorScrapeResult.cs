using System;
using System.Collections.Generic;

namespace Knapcode.ConnectorRide.Core
{
    public class ConnectorScrapeResult
    {
        public DateTimeOffset StartTime { get; set; }
        public IEnumerable<Schedule> Schedules { get; set; }
        public DateTimeOffset EndTime { get; set; }
    }
}