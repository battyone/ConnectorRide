namespace Knapcode.ConnectorRide.Core.GtfsModels
{
    public struct Date
    {
        public Date(int year, int month, int day)
        {
            Year = year;
            Month = month;
            Day = day;
        }

        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
    }
}