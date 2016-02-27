using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CsvHelper;
using Knapcode.ConnectorRide.Core.GtfsModels;

namespace Knapcode.ConnectorRide.Core
{
    public interface IGtfsCsvSerializer
    {
        void SerializeAgencies(Stream stream, Agency[] records);
        void SerializeStops(Stream stream, Stop[] records);
        void SerializeRoutes(Stream stream, Route[] records);
        void SerializeTrips(Stream stream, Trip[] records);
        void SerializeCalendar(Stream stream, Service[] records);
        void SerializeStopTimes(Stream stream, StopTime[] records);
        void SerializeShapes(Stream stream, ShapePoint[] records);
    }

    public class GtfsCsvSerializer : IGtfsCsvSerializer
    {
        public void SerializeAgencies(Stream stream, Agency[] records)
        {
            SerializeCsv(
                stream,
                records,
                csvWriter =>
                {
                    csvWriter.WriteField("agency_name");
                    csvWriter.WriteField("agency_url");
                    csvWriter.WriteField("agency_timezone");
                    csvWriter.WriteField("agency_phone");
                },
                (csvWriter, record) =>
                {
                    csvWriter.WriteField(record.Name);
                    csvWriter.WriteField(record.Url);
                    csvWriter.WriteField(record.Timezone);
                    csvWriter.WriteField(record.Phone);
                    csvWriter.NextRecord();
                });
        }

        public void SerializeStops(Stream stream, Stop[] records)
        {
            SerializeCsv(
                stream,
                records,
                csvWriter =>
                {
                    csvWriter.WriteField("stop_id");
                    csvWriter.WriteField("stop_name");
                    csvWriter.WriteField("stop_desc");
                    csvWriter.WriteField("stop_lat");
                    csvWriter.WriteField("stop_lon");
                },
                (csvWriter, record) =>
                {
                    csvWriter.WriteField(record.Id);
                    csvWriter.WriteField(record.Name);
                    csvWriter.WriteField(record.Desc);
                    csvWriter.WriteField(record.Lat);
                    csvWriter.WriteField(record.Lon);
                });
        }

        public void SerializeRoutes(Stream stream, Route[] records)
        {
            SerializeCsv(
                stream,
                records,
                csvWriter =>
                {
                    csvWriter.WriteField("route_id");
                    csvWriter.WriteField("route_short_name");
                    csvWriter.WriteField("route_long_name");
                    csvWriter.WriteField("route_type");
                },
                (csvWriter, record) =>
                {
                    csvWriter.WriteField(record.Id);
                    csvWriter.WriteField(string.Empty);
                    csvWriter.WriteField(record.LongName);
                    csvWriter.WriteField((int?) record.Type);
                });
        }

        public void SerializeTrips(Stream stream, Trip[] records)
        {
            SerializeCsv(
                stream,
                records,
                csvWriter =>
                {
                    csvWriter.WriteField("route_id");
                    csvWriter.WriteField("service_id");
                    csvWriter.WriteField("trip_id");
                    csvWriter.WriteField("direction_id");
                    csvWriter.WriteField("shape_id");
                },
                (csvWriter, record) =>
                {
                    csvWriter.WriteField(record.RouteId);
                    csvWriter.WriteField(record.ServiceId);
                    csvWriter.WriteField(record.Id);
                    csvWriter.WriteField(record.DirectionId.HasValue ? SerializeBoolean(record.DirectionId.Value) : string.Empty);
                    csvWriter.WriteField(record.ShapeId);
                });
        }

        public void SerializeCalendar(Stream stream, Service[] records)
        {
            SerializeCsv(
                stream,
                records,
                csvWriter =>
                {
                    csvWriter.WriteField("service_id");
                    csvWriter.WriteField("monday");
                    csvWriter.WriteField("tuesday");
                    csvWriter.WriteField("wednesday");
                    csvWriter.WriteField("thursday");
                    csvWriter.WriteField("friday");
                    csvWriter.WriteField("saturday");
                    csvWriter.WriteField("sunday");
                    csvWriter.WriteField("start_date");
                    csvWriter.WriteField("end_date");
                },
                (csvWriter, record) =>
                {
                    csvWriter.WriteField(record.Id);
                    csvWriter.WriteField(SerializeBoolean(record.Monday));
                    csvWriter.WriteField(SerializeBoolean(record.Tuesday));
                    csvWriter.WriteField(SerializeBoolean(record.Wednesday));
                    csvWriter.WriteField(SerializeBoolean(record.Thursday));
                    csvWriter.WriteField(SerializeBoolean(record.Friday));
                    csvWriter.WriteField(SerializeBoolean(record.Saturday));
                    csvWriter.WriteField(SerializeBoolean(record.Sunday));
                    csvWriter.WriteField(SerializeDate(record.StartDate));
                    csvWriter.WriteField(SerializeDate(record.EndDate));
                });
        }

        public void SerializeStopTimes(Stream stream, StopTime[] records)
        {
            SerializeCsv(
                stream,
                records,
                csvWriter =>
                {
                    csvWriter.WriteField("trip_id");
                    csvWriter.WriteField("arrival_time");
                    csvWriter.WriteField("departure_time");
                    csvWriter.WriteField("stop_id");
                    csvWriter.WriteField("stop_sequence");
                    csvWriter.WriteField("pickup_type");
                    csvWriter.WriteField("drop_off_type");
                },
                (csvWriter, record) =>
                {
                    csvWriter.WriteField(record.TripId);
                    csvWriter.WriteField(SerializeTime(record.ArrivalTime));
                    csvWriter.WriteField(SerializeTime(record.DepartureTime));
                    csvWriter.WriteField(record.StopId);
                    csvWriter.WriteField(record.Sequence);
                    csvWriter.WriteField((int?) record.PickupType);
                    csvWriter.WriteField((int?) record.DropOffType);
                });
        }

        public void SerializeShapes(Stream stream, ShapePoint[] records)
        {
            SerializeCsv(
                stream,
                records,
                csvWriter =>
                {
                    csvWriter.WriteField("shape_id");
                    csvWriter.WriteField("shape_pt_lat");
                    csvWriter.WriteField("shape_pt_lon");
                    csvWriter.WriteField("shape_pt_sequence");
                },
                (csvWriter, record) =>
                {
                    csvWriter.WriteField(record.ShapeId);
                    csvWriter.WriteField(record.Lat);
                    csvWriter.WriteField(record.Lon);
                    csvWriter.WriteField(record.Sequence);
                });
        }

        private string SerializeTime(Time value)
        {
            return $"{value.Hour:D2}:{value.Minute:D2}:{value.Second:D2}";
        }

        private string SerializeBoolean(bool value)
        {
            return value ? "1" : "0";
        }

        private string SerializeDate(Date value)
        {
            return $"{value.Year:D4}{value.Month:D2}{value.Day:D2}";
        }

        private static void SerializeCsv<T>(Stream stream, IEnumerable<T> records, Action<CsvWriter> writeHeader, Action<CsvWriter, T> writeRecord)
        {
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
            using (var csvWriter = new CsvWriter(writer))
            {
                writeHeader(csvWriter);
                csvWriter.NextRecord();

                foreach (var record in records)
                {
                    writeRecord(csvWriter, record);
                    csvWriter.NextRecord();
                }
            }
        }
    }
}