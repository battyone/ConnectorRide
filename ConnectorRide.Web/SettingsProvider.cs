using System.Configuration;

namespace Knapcode.ConnectorRide.Web
{
    public interface ISettingsProvider
    {
        string GetValue(string key);
    }

    public class SettingsProvider : ISettingsProvider
    {
        public string GetValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}