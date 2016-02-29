using System;

namespace Knapcode.ConnectorRide.Web
{
    public interface ISettings
    {
        string ScrapeResultPathFormat { get; }
        TimeSpan SchedulesMaximumScrapeFrequency { get; }
        string StorageContainer { get; }
        string StorageConnectionString { get; }
        string GtfsFeedArchiveGroupedPathFormat { get; }
        string GtfsFeedArchiveUngroupedPathFormat { get; }
    }

    public class Settings : ISettings
    {
        private readonly SettingsProvider _provider;

        public Settings(SettingsProvider provider)
        {
            _provider = provider;
        }

        public string ScrapeResultPathFormat => _provider.GetValue("ConnectorRide:ScrapeResult:PathFormat") ?? "schedules/{0}.json";

        public string GtfsFeedArchiveGroupedPathFormat => _provider.GetValue("ConnectorRide:GtfsFeedArchive:GroupedPathFormat") ?? "gtfs/{0}.zip";

        public string GtfsFeedArchiveUngroupedPathFormat => _provider.GetValue("ConnectorRide:GtfsFeedArchive:UngroupedPathFormat") ?? "gtfs-ungrouped/{0}.zip";

        public TimeSpan SchedulesMaximumScrapeFrequency
        {
            get
            {
                var value = _provider.GetValue("ConnectorRide:ScrapeResult:MaximumScrapeFrequency");
                if (value == null)
                {
                    return TimeSpan.FromHours(12);
                }

                return TimeSpan.Parse(value);
            }
        }

        public string StorageContainer => _provider.GetValue("ConnectorRide:Storage:Container") ?? "scrape";

        public string StorageConnectionString => _provider.GetValue("ConnectorRide:Storage:ConnectionString");
    }
}