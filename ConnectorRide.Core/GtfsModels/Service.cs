namespace Knapcode.ConnectorRide.Core.GtfsModels
{
    public class Service
    {
        public string Id { get; set; }
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }
        public Date StartDate { get; set; }
        public Date EndDate { get; set; }
    }
}