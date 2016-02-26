namespace Knapcode.ConnectorRide.Core.GtfsModels
{
    public struct Time
    {
        public Time(int hour, int minute, int second)
        {
            Hour = hour;
            Minute = minute;
            Second = second;
        }

        public int Hour { get; }
        public int Minute { get; }
        public int Second { get; }
    }
}