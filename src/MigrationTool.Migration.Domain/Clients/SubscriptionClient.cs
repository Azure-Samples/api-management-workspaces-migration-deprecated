using System.Net.Http.Json;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.TemplateModels;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiOperations;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients;

public class SubscriptionClient : ClientBase
{
    const string FetchForApiRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/subscriptions?$filter=properties/scope eq '/apis/{4}'&api-version={5}";

    const string FetchForProductRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/subscriptions?$filter=properties/scope eq '/products/{4}'&api-version={5}";

    const string CreateRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/subscriptions/{5}?api-version={6}";

    public SubscriptionClient(IHttpClientFactory httpClientFactory,
        ExtractorParameters extractorParameters,
        IApiDataProcessor apiDataProcessor,
        IApiRevisionClient apiRevisionClient)
        : base(httpClientFactory, extractorParameters, apiDataProcessor, apiRevisionClient)
    {
    }

    public Task<IReadOnlyCollection<Entity>> FetchForApi(string entityId) =>
        this.Fetch(entityId, FetchForApiRequest);

    public Task<IReadOnlyCollection<Entity>> FetchForProduct(string entityId) =>
        this.Fetch(entityId, FetchForProductRequest);

    private async Task<IReadOnlyCollection<Entity>> Fetch(string entityId, string urlFormat)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(urlFormat,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            entityId, GlobalConstants.ApiVersion);

        var response = await this.GetPagedResponseAsync<SubscriptionsTemplateResource>(azToken, requestUrl);

        return response
            .Where(s => !s.Properties.ownerId.EndsWith("/users/1")) // skip subscription for admin user
            .Select(s => new Entity(s.Name, EntityType.Subscription, s.Properties.displayName, s))
            .ToList();
    }

    public async Task<Entity> Create(Entity entity, string workspaceId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspaceId, entity.Id, GlobalConstants.ApiVersion);
        
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content =
            JsonContent.Create<SubscriptionsTemplateResource>((SubscriptionsTemplateResource)entity.ArmTemplate,
                options: DefaultSerializerOptions);
        
        var response = await this.CallApiManagementAsync(azToken, request);
        var armTemplate = response.Deserialize<SubscriptionsTemplateResource>();
        return new Entity(armTemplate.Name, EntityType.Subscription, armTemplate.Properties.displayName, armTemplate);
    }
}