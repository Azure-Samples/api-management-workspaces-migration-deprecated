using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor.Operations;
using MT =  Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;

namespace MigrationTool.Migration.Domain.Clients;

public class ApiClient : ClientBase, IApiClient
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

    const string ExportApiRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/apis/{4}?export=true&format=openapi%2Bjson&api-version={5}";

    const string ImportApiRequest = "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/apis/{5}?import=true&api-version={6}";


    const string AddTagRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/apis/{5}/tags/{6}?api-version={7}";


    private readonly IApisClient ApisClient;
    private readonly IProductsClient ProductsClient;
    private readonly IApiOperationClient ApiOperationClient;
    private readonly IPolicyClient PolicyClient;
    private readonly MT.ITagClient TagClient;

    public ApiClient(
        ExtractorParameters extractorParameters,
        IApisClient apisClient, IProductsClient productsClient,
        IApiOperationClient apiOperationClient,
        IPolicyClient policyClient,
        IHttpClientFactory httpClientFactory,
        EntitiesRegistry registry,
        MT.ITagClient tagClient,
        AzureCliAuthenticator auth = null
        )
        : base(httpClientFactory, extractorParameters, auth)
    {
        this.ApisClient = apisClient;
        this.ProductsClient = productsClient;
        this.ApiOperationClient = apiOperationClient;
        this.PolicyClient = policyClient;
        this.TagClient = tagClient;
    }

    public async Task<IReadOnlyCollection<Entity>> FetchAllApisAndVersionSets()
    {
        var apis = await this.ApisClient.GetAllAsync(this.ExtractorParameters);

        return await this.ProcessApiData(apis);
    }

    public async Task<IReadOnlyCollection<Entity>> FetchAllApisFlat()
    {
        var apis = await this.ApisClient.GetAllAsync(this.ExtractorParameters);
        return this.CreateApiEntities(apis);
    }

    public async Task<IReadOnlyCollection<Entity>> FetchProducts(string entityId)
    {
        var products = await this.ProductsClient.GetAllLinkedToApiAsync(entityId, this.ExtractorParameters);
        return products.ConvertAll(product =>
            new Entity(product.Name, EntityType.Product, product.Properties.DisplayName, product));
    }

    public async Task<Entity> Fetch(string apiId)
    {
        var apis = await this.FetchAllApisFlat();
        return apis.Where(api => api.Id.Equals(apiId)).First();
    }

    public async Task<string?> FetchPolicy(string entityId)
    {
        var policy = await this.PolicyClient.GetPolicyLinkedToApiAsync(entityId, this.ExtractorParameters);
        return policy?.Properties?.PolicyContent;
    }

    public async Task<IReadOnlyCollection<Entity>> FetchTags(string entityId)
    {
        var tags = await this.TagClient.GetAllTagsLinkedToApiAsync(entityId, this.ExtractorParameters);
        return tags.ConvertAll(tag =>
            new Entity(tag.Properties.DisplayName, EntityType.Tag, tag.Properties.DisplayName, tag));
    }

    public async Task<IReadOnlyCollection<Entity>> FetchOperations(string entityId)
    {
        var operations =
            await this.ApiOperationClient.GetOperationsLinkedToApiAsync(entityId, this.ExtractorParameters);
         return operations.ConvertAll(operation =>
            new OperationEntity(operation.Name, entityId));
    }

    public async Task<string?> FetchOperationPolicy(string entityId, string operationId)
    {
        var policy =
            await this.PolicyClient.GetPolicyLinkedToApiOperationAsync(entityId, operationId, this.ExtractorParameters);
        return policy?.Properties?.PolicyContent;
    }

    public async Task<IReadOnlyCollection<Entity>> FetchOperationTags(string entityId, string operationId)
    {
        var tags = await this.TagClient.GetTagsLinkedToApiOperationAsync(entityId, operationId, this.ExtractorParameters);
        return tags.ConvertAll(tag =>
            new Entity(tag.Properties.DisplayName, EntityType.Tag, tag.Properties.DisplayName, tag));
    }

    public async Task<string> ExportOpenApiDefinition(string apiId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(ExportApiRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            apiId, GlobalConstants.ApiVersion);
        return await this.CallApiManagementAsync(azToken, requestUrl);
    }

    public async Task<Entity> ImportOpenApiDefinition(string apiDefinition, string apiId, string workspace)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        var requestUrl = string.Format(ImportApiRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspace, apiId, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = new StringContent(apiDefinition, Encoding.UTF8, "application/json");
        var creationResponse = await this.CallApiManagementAsync(azToken, request);

        Entity newApi = null;
        HttpRequestMessage newApiReq = new HttpRequestMessage(HttpMethod.Get, creationResponse.Headers.Location);
        var status = HttpStatusCode.NotFound;
        var tryCount = 5;
        while (status == HttpStatusCode.NotFound && tryCount > 0)
        {
            tryCount--;
            var isReady = await this.CallApiManagementAsync(azToken, newApiReq);
            status = isReady.StatusCode;
            if (status == HttpStatusCode.Created)
            {
                string responseBody = await isReady.Content.ReadAsStringAsync();
                var armTemplate = responseBody.Deserialize<ApiTemplateResource>();
                newApi = new Entity(armTemplate.Name, EntityType.Api, armTemplate.Properties.DisplayName, armTemplate);
            }
            else
            {
                Thread.Sleep(100);
            }
        }
        return newApi;
    }


    public async Task<Entity> Create(ApiTemplateResource resource, string workspace) =>
        await CreateOrUpdateApi(resource, workspace);


    private async Task<Entity> CreateOrUpdateApi(ApiTemplateResource resource, string workspace)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateApiRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspace, resource.Name, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = JsonContent.Create(resource, options: DefaultSerializerOptions);
        var response = await this.GetResponseBodyAsync(azToken, request);
        var armTemplate = response.Deserialize<ApiTemplateResource>();
        return new Entity(armTemplate.Name, EntityType.Api, armTemplate.Properties.DisplayName, armTemplate);
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

    public async Task UploadApiOperationPolicy(string apiId, string operationId, string policy, string workspace)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateApiOperationPolicyRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspace, apiId, operationId, GlobalConstants.ApiVersion);

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