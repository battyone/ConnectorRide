using Knapcode.ConnectorRide.Core.ClientModels;

namespace Knapcode.ConnectorRide.Core.ScraperModels
{
    public class Schedule
    {
        public string Name { get; set; }
        public Table Table { get; set; }
        public Map Map { get; set; }
    }
}