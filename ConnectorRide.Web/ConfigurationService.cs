using System.Configuration;

namespace Knapcode.ConnectorRide.Web
{
    public class ConfigurationService
    {
        public string StoragePathFormat => ConfigurationManager.AppSettings["ConnectorRide:StoragePathFormat"];

        public string StorageContainer => ConfigurationManager.AppSettings["ConnectorRide:StorageContainer"];

        public string StorageConnectionString => ConfigurationManager.AppSettings["ConnectorRide:StorageConnectionString"];
    }
}