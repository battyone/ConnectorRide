using System;
using Knapcode.ConnectorRide.Core.ClientModels;

namespace Knapcode.ConnectorRide.Core
{
    public interface IScrapeResultWriter
    {
        void WriteStart();
        void WriteVersion(string version);
        void WriteStartTime(DateTimeOffset startTime);
        void WriteStartSchedules();
        void WriteStartSchedule();
        void WriteScheduleName(string name);
        void WriteScheduleTable(Table table);
        void WriteScheduleMap(Map map);
        void WriteEndSchedule();
        void WriteEndSchedules();
        void WriteEndTime(DateTimeOffset endTime);
        void WriteEnd();
    }
}