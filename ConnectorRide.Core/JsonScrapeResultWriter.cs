using System;
using Knapcode.ConnectorRide.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Knapcode.ConnectorRide.Core
{
    public class JsonScrapeResultWriter : IScrapeResultWriter
    {
        private readonly JsonWriter _writer;

        public JsonScrapeResultWriter(JsonWriter writer)
        {
            _writer = writer;
        }

        public void WriteStart()
        {
            _writer.WriteStartObject();
        }

        public void WriteVersion(string version)
        {
            _writer.WritePropertyName("Version");
            _writer.WriteValue(version);
        }

        public void WriteStartTime(DateTimeOffset startTime)
        {
            _writer.WritePropertyName("StartTime");
            _writer.WriteValue(startTime);
        }

        public void WriteStartSchedules()
        {
            _writer.WritePropertyName("Schedules");
            _writer.WriteStartArray();
        }

        public void WriteStartSchedule()
        {
            _writer.WriteStartObject();
        }

        public void WriteScheduleName(string name)
        {
            _writer.WritePropertyName("Name");
            _writer.WriteValue(name);
        }

        public void WriteScheduleTable(Table table)
        {
            _writer.WritePropertyName("Table");
            JObject.FromObject(table).WriteTo(_writer);
        }

        public void WriteScheduleMap(Map map)
        {
            _writer.WritePropertyName("Map");
            JObject.FromObject(map).WriteTo(_writer);
        }

        public void WriteEndSchedule()
        {
            _writer.WriteEndObject();
        }

        public void WriteEndSchedules()
        {
            _writer.WriteEndArray();
        }

        public void WriteEndTime(DateTimeOffset endTime)
        {
            _writer.WritePropertyName("EndTime");
            _writer.WriteValue(endTime);
        }

        public void WriteEnd()
        {
            _writer.WriteEndObject();
        }
    }
}