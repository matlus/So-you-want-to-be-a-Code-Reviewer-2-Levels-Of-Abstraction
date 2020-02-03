using DomainLayer.Managers.ConfigurationProviders;
using DomainLayer.Managers.SegregatedInterfaces;
using DomainLayer.Managers.Services.ImdbService;
using System.Net.Http;

namespace DomainLayer.Managers.ServiceLocators
{
    internal abstract class ServiceLocatorBase : IHttpMessageHandlerProvider
    {
        public MovieManager CreateMovieManager()
        {
            return CreateMovieManagerCore();
        }

        public ImdbServiceGateway CreateImdbServiceGateway(string baseUrl)
        {
            return CreateImdbServiceGatewayCore(baseUrl);
        }

        public ConfigurationProviderBase CreateConfigurationProvider()
        {
            return CreateConfigurationProviderCore();
        }

        public HttpMessageHandler CreateHttpMessageHandler()
        {
            return CreateHttpMessageHandlerCore();
        }

        protected abstract HttpMessageHandler CreateHttpMessageHandlerCore();
        protected abstract ConfigurationProviderBase CreateConfigurationProviderCore();
        protected abstract ImdbServiceGateway CreateImdbServiceGatewayCore(string baseUrl);
        protected abstract MovieManager CreateMovieManagerCore();
    }
}
