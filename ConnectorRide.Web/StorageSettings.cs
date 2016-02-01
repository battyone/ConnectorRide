namespace Knapcode.ConnectorRide.Web
{
    public class StorageSettings
    {
        private readonly SettingsService _service;

        public StorageSettings(SettingsService service)
        {
            _service = service;
        }

        public string SchedulePathFormat => _service.GetValue("ConnectorRide:Storage:SchedulePathFormat") ?? "schedules/{0}.json";

        public string Container => _service.GetValue("ConnectorRide:Storage:Container") ?? "scrape";

        public string ConnectionString => _service.GetValue("ConnectorRide:Storage:ConnectionString");
    }
}