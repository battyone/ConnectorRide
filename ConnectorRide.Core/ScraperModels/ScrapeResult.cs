using System;
using System.Collections.Generic;

namespace Knapcode.ConnectorRide.Core.ScraperModels
{
    public class ScrapeResult
    {
        public string Version { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public IEnumerable<Schedule> Schedules { get; set; }
        public DateTimeOffset EndTime { get; set; }
    }
}
