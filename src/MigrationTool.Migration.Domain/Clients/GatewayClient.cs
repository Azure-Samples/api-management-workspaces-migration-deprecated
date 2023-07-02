using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Gateway;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients;

public class GatewayClient : ClientBase, MigrationTool.Migration.Domain.Clients.Abstraction.IGatewayClient
{
    private IGatewayClient gatewayClient;
    private IApiDataProcessor apiDataProcessor;

    const string GetApisRequest =
    "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/gateways/{4}/apis?api-version={5}";
    public GatewayClient(IHttpClientFactory httpClientFactory, ExtractorParameters extractorParameters, IGatewayClient gatewayClient, IApiDataProcessor apiDataProcessor) : base(httpClientFactory, extractorParameters)
    {
        this.gatewayClient = gatewayClient;
        this.apiDataProcessor = apiDataProcessor;
    }

    private async Task<IReadOnlyCollection<Entity>> FetchApis(string id)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(GetApisRequest, this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName, id, GlobalConstants.ApiVersion);
        List <ApiTemplateResource> apis = await this.GetPagedResponseAsync<ApiTemplateResource>(azToken, requestUrl);
        this.apiDataProcessor.ProcessData(apis);
        return await this.ProcessApiData(apis);
    }

    private async Task<IReadOnlyCollection<Entity>> FetchAllApisLinkedToGateways()
    {
        List<GatewayTemplateResource> gateways = await this.gatewayClient.GetAllAsync(this.ExtractorParameters);
        List<Entity> allApis = new List<Entity>();
        foreach (var gateway in gateways)
        {
            var current = await this.FetchApis(gateway.Name);
            allApis.AddRange(current);
        }

        return allApis;
    }

    public async Task<bool> IsLinkedWithGateway(ApiEntity api)
    {
        var apis = await this.FetchAllApisLinkedToGateways();
        return apis.Contains(api);
    }
}
