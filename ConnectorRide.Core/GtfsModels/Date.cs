using System;

namespace Knapcode.ConnectorRide.Core.GtfsModels
{
    public struct Date
    {
        public Date(int year, int month, int day)
        {
            // Use a DateTimeOffset for validating the input.
            var dateTimeOffset = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);

            Year = dateTimeOffset.Year;
            Month = dateTimeOffset.Month;
            Day = dateTimeOffset.Day;
        }

        public int Year { get; }
        public int Month { get; }
        public int Day { get; }
    }
}