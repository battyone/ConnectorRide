using System;
using System.Collections.Generic;
using Knapcode.ConnectorRide.Core.ClientModels;

namespace Knapcode.ConnectorRide.Core
{
    public class ScraperResult
    {
        public DateTimeOffset StartTime { get; set; }
        public IEnumerable<Schedule> Schedules { get; set; }
        public DateTimeOffset EndTime { get; set; }
    }
}