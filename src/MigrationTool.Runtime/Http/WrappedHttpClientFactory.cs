namespace MigrationTool.Http
{
    public class WrappedHttpClientFactory : IHttpClientFactory
    {
        private IApplicationInfo applicationInfo;
        private IHttpClientFactory innerFactory;

        public WrappedHttpClientFactory(IApplicationInfo applicationInfo, IHttpClientFactory innerFactory)
        {
            this.applicationInfo = applicationInfo;
            this.innerFactory = innerFactory;
        }
        public HttpClient CreateClient(string name)
        {
            var httpClient = this.innerFactory.CreateClient(name);
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd($"{applicationInfo.Name}/{applicationInfo.BuildVersion}");
            
            return httpClient;
        }
    }
}