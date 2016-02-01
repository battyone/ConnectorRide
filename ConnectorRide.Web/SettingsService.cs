using System.Configuration;

namespace Knapcode.ConnectorRide.Web
{
    public class SettingsService
    {
        public string GetValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}