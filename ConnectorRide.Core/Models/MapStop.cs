namespace Knapcode.ConnectorRide.Core.Models
{
    public class MapStop
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Description { get; set; }
        public object ZipCode { get; set; }
        public bool IsPick { get; set; }
        public bool IsDrop { get; set; }
        public bool IsHub { get; set; }
        public object RoutesServiced { get; set; }
    }
}