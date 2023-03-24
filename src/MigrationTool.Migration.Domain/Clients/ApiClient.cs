using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiOperations;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Entities;
using System.Net.Http.Json;
using MigrationTool.Migration.Domain.Extensions;

namespace MigrationTool.Migration.Domain.Clients;

public class ApiClient : ClientBase
{
    const string CreateApiRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/apis/{5}?api-version={6}";

    const string CreateApiOperationsRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/apis/{5}/operations/{6}?api-version={7}";

    const string CreateApiPolicyRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/apis/{5}/policies/policy?api-version={6}";

    const string CreateApiOperationPolicyRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/apis/{5}/operations/{6}/policies/policy?api-version={7}";

    const string DeleteApiRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/apis/{4}?api-version={5}";

    private readonly IApisClient ApisClient;
    private readonly IProductsClient ProductsClient;
    private readonly IApiOperationClient ApiOperationClient;
    private readonly IPolicyClient PolicyClient;
    private readonly IApiVersionSetClient ApiVersionSetClient;

    public ApiClient(
        ExtractorParameters extractorParameters,
        IApisClient apisClient, IProductsClient productsClient,
        IApiOperationClient apiOperationClient,
        IApiRevisionClient apiRevisionClient,
        IApiVersionSetClient apiVersionSetClient,
        IPolicyClient policyClient,
        IHttpClientFactory httpClientFactory,
        IApiDataProcessor apiDataProcessor)
        : base(httpClientFactory, extractorParameters, apiDataProcessor, apiRevisionClient)
    {
        this.ApisClient = apisClient;
        this.ProductsClient = productsClient;
        this.ApiOperationClient = apiOperationClient;
        this.PolicyClient = policyClient;
        this.ApiVersionSetClient = apiVersionSetClient;
    }

    public async Task<IReadOnlyCollection<Entity>> FetchAllApis()
    {
        var apis = await this.ApisClient.GetAllCurrentAsync(this.ExtractorParameters);

        return await this.RemoveUnsupportedApis(apis);
    }

    public async Task<IReadOnlyCollection<Entity>> FetchApiRevisions(string apiId)
    {
        var revisions = await this.ApiRevisionClient.GetApiRevisionsAsync(apiId, this.ExtractorParameters);
        return revisions.ConvertAll(api => new Entity(api.ApiId, api.ApiRevision, EntityType.Api, null));
    }

    public async Task<IReadOnlyCollection<Entity>> FetchProducts(string entityId)
    {
        var products = await this.ProductsClient.GetAllLinkedToApiAsync(entityId, this.ExtractorParameters);
        return products.ConvertAll(product =>
            new Entity(product.Name, product.Properties.DisplayName, EntityType.Product, product));
    }

    public async Task<string?> FetchPolicy(string entityId)
    {
        var policy = await this.PolicyClient.GetPolicyLinkedToApiAsync(entityId, this.ExtractorParameters);
        return policy?.Properties?.PolicyContent;
    }

    public Task<IReadOnlyCollection<Entity>> FetchTags(string entityId)
    {
        // TODO: Implement
        return Task.FromResult<IReadOnlyCollection<Entity>>(new List<Entity>());
    }

    public async Task<IReadOnlyCollection<Entity>> FetchOperations(string entityId)
    {
        var operations =
            await this.ApiOperationClient.GetOperationsLinkedToApiAsync(entityId, this.ExtractorParameters);
        return operations.ConvertAll(operation => new Entity(operation.Name,
            operation.Properties.DisplayName, EntityType.ApiOperation, operation));
    }

    public async Task<string?> FetchOperationPolicy(string entityId, string operationId)
    {
        var policy =
            await this.PolicyClient.GetPolicyLinkedToApiOperationAsync(entityId, operationId, this.ExtractorParameters);
        return policy?.Properties?.PolicyContent;
    }

    public Task<IReadOnlyCollection<Entity>> FetchOperationTags(string entityId, string operationId)
    {
        // TODO: Implement
        return Task.FromResult<IReadOnlyCollection<Entity>>(new List<Entity>());
    }


    public async Task<Entity> Create(Entity sourceEntity, Func<string, string> modifier, string workspace)
    {
        return await CreateOrUpdateApi(sourceEntity, modifier, modifier(sourceEntity.Id), workspace);
    }

    public async Task<Entity> Update(Entity sourceEntity, Func<string, string> modifier, string workspace)
    {
        return await CreateOrUpdateApi(sourceEntity, modifier, sourceEntity.Id, workspace);
    }

    private async Task<Entity> CreateOrUpdateApi(Entity sourceEntity, Func<string, string> modifier, string newId,
        string workspace)
    {
        if (sourceEntity.Type != EntityType.Api)
            throw new ArgumentException("Provided entity should be of type Api");

        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateApiRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspace, newId, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);

        var apiTemplate = ((ApiTemplateResource)sourceEntity.ArmTemplate).Copy();
        apiTemplate.Name = null;
        apiTemplate.Properties.DisplayName = modifier(apiTemplate.Properties.DisplayName);
        apiTemplate.Properties.Path = modifier(apiTemplate.Properties.Path);

        request.Content = JsonContent.Create<ApiTemplateResource>(apiTemplate, options: DefaultSerializerOptions);
        var response = await this.CallApiManagementAsync(azToken, request);
        var armTemplate = response.Deserialize<ApiTemplateResource>();
        return new Entity(armTemplate.Name, armTemplate.Properties.DisplayName, EntityType.Api, armTemplate);
    }

    public async Task<Entity> CreateOperation(Entity sourceEntity, Entity api, string workspace)
    {
        if (sourceEntity.Type != EntityType.ApiOperation)
            throw new ArgumentException("Provided entity should be of type ApiOperation");

        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateApiOperationsRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspace, api.Id, sourceEntity.Id, GlobalConstants.ApiVersion);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content =
            JsonContent.Create<ApiOperationTemplateResource>((ApiOperationTemplateResource)sourceEntity.ArmTemplate,
                options: DefaultSerializerOptions);
        var response = await this.CallApiManagementAsync(azToken, request);
        var armTemplate = response.Deserialize<ApiOperationTemplateResource>();
        return new Entity(armTemplate.Name, armTemplate.Properties.DisplayName, EntityType.ApiOperation, armTemplate);
    }

    public async Task UploadApiPolicy(Entity api, string policy, string workspace)
    {
        if (api.Type != EntityType.Api)
            throw new ArgumentException("Provided entity should be of type Api");

        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateApiPolicyRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspace, api.Id, GlobalConstants.ApiVersion);

        await UploadPolicy(requestUrl, policy);
    }

    public async Task UploadApiOperationPolicy(Entity api, Entity operation, string policy, string workspace)
    {
        if (api.Type != EntityType.Api)
            throw new ArgumentException("Provided entity should be of type Api");
        if (operation.Type != EntityType.ApiOperation)
            throw new ArgumentException("Provided entity should be of type ApiOperation");

        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateApiOperationPolicyRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspace, api.Id, operation.Id, GlobalConstants.ApiVersion);

        await UploadPolicy(requestUrl, policy);
    }

    public async Task Delete(Entity api)
    {
        if (api.Type != EntityType.Api)
            throw new ArgumentException("Provided entity should be of type Api");

        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(DeleteApiRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            api.Id, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
        await this.CallApiManagementAsync(azToken, request);
    }
}