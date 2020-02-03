using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace DomainLayer.Managers.ConfigurationProviders
{
    internal sealed class ConfigurationProvider : ConfigurationProviderBase
    {
        private readonly IConfigurationRoot _configurationRoot;

        public ConfigurationProvider()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            _configurationRoot = configurationBuilder.Build();
        }

        [ExcludeFromCodeCoverage]
        internal ConfigurationProvider(IConfigurationRoot configurationRoot)
        {
            _configurationRoot = configurationRoot;
        }

        protected override string RetrieveConfigurationSettingValue(string key)
        {
            return _configurationRoot["AppSettings:" + key];
        }
    }
}
