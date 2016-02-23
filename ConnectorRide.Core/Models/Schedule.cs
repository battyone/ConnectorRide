using System.Collections.Generic;

namespace Knapcode.ConnectorRide.Core.Models
{
    public class Schedule
    {
        public string Name { get; set; }
        public Map Map { get; set; }
        public Table Table { get; set; }
    }
}