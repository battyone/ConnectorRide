using System;

namespace Knapcode.ConnectorRide.Web.Settings
{
    public class ConnectorRideSettings
    {
        private readonly SettingsService _service;

        public ConnectorRideSettings(SettingsService service)
        {
            _service = service;
        }

        public string SchedulesPathFormat => _service.GetValue("ConnectorRide:Schedules:PathFormat") ?? "schedules/{0}.json";

        public TimeSpan SchedulesMaximumScrapeFrequency
        {
            get
            {
                var value = _service.GetValue("ConnectorRide:Schedules:MaximumScrapeFrequency");
                if (value == null)
                {
                    return TimeSpan.FromHours(1);
                }

                return TimeSpan.Parse(value);
            }
        }

        public string StorageContainer => _service.GetValue("ConnectorRide:Storage:Container") ?? "scrape";

        public string StorageConnectionString => _service.GetValue("ConnectorRide:Storage:ConnectionString");

    }
}