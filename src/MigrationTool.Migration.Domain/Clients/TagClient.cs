using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ProductApis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Tags;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities;
using MigrationTool.Migration.Domain.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;

namespace MigrationTool.Migration.Domain.Clients;

public class TagClient : ClientBase
{

    const string FetchEntitiesAssociatedWithTagRequest = "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/tagResources?$filter=tag/name eq '{4}'&api-version={5}";
    const string CreateRequest = "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/tags/{5}?api-version={6}";
    const string LinkWithApiRequest = "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/tags/{5}/apiLinks/{6}?api-version={7}";
    const string LinkWithProductRequest = "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/tags/{5}/productLinks/{6}?api-version={7}";
    const string LinkWithApiOperationRequest = "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/tags/{5}/operationLinks/{6}?api-version={7}";

    const string IdString = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.ApiManagement/service/{2}/workspaces/{3}/{4}/{5}";

    private ProductClient _productClient;
    private ApiClient _apiClient;

    public TagClient(IHttpClientFactory httpClientFactory, ExtractorParameters extractorParameters, ProductClient productClient, ApiClient apiClient, AzureCliAuthenticator auth = null) : base(httpClientFactory, extractorParameters, auth)
    {
        this._productClient = productClient;
        this._apiClient = apiClient;
    }

    public async Task<IReadOnlyCollection<Entity>> FetchEntities(string tagId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(FetchEntitiesAssociatedWithTagRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            tagId, GlobalConstants.ApiVersion);
        var result =  await this.CallApiManagementAsync(azToken, requestUrl);

        JObject deserialized = (JObject)JsonConvert.DeserializeObject<object>(result);

        List<Entity> entities = new();
        foreach (var child in deserialized.SelectToken("value").Children())
        {
            entities.Add(await extractEntity((JObject)child));
        }
        return entities;
    }

    private async Task<Entity> extractEntity(JObject entry)
    {
        EntityType type;

        var innerObject = entry.Value<JObject?>("api") ?? entry.Value<JObject?>("product") ?? entry.Value<JObject?>("operation");
        var id = innerObject.Value<string>("id");
        id = id.Substring(id.LastIndexOf("/") + 1);

        if (entry.Value<JObject?>("api") != null)
        {
            return await this._apiClient.Fetch(id);
        } else if (entry.Value<JObject?>("product") != null)
        {
            return await this._productClient.Fetch(id);
        } else
        {
            //return await this._apiClient.Fetch(innerObject.Value<string>("apiName"));
            var operations = await this._apiClient.FetchOperations(innerObject.Value<string>("apiName"));
            return operations.Where(operation => operation.Id.Equals(id)).First();
        }
    }
    internal async Task Create(TagTemplateResource resource, string workspaceId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspaceId, resource.Properties.DisplayName, GlobalConstants.ApiVersion);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = JsonContent.Create(resource, options: DefaultSerializerOptions);

        await this.CallApiManagementAsync(azToken, request);
    }

    internal async Task ConnectWithProduct(Entity tag, Entity product, string workspaceId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        var productId = string.Format(IdString,
            azSubId, this.ExtractorParameters.ResourceGroup,
            this.ExtractorParameters.SourceApimName, workspaceId, "products", product.Id
            );
        var payload = new { Properties = new { productId = productId } };
        await this.Connect(tag, workspaceId, LinkWithProductRequest, payload);
    }

    internal async Task ConnectWithApi(Entity tag, Entity api, string workspaceId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        var apiId = string.Format(IdString,
            azSubId, this.ExtractorParameters.ResourceGroup,
            this.ExtractorParameters.SourceApimName, workspaceId, "apis", api.Id
            );
        var payload = new { Properties = new { apiId = apiId } };
        await this.Connect(tag, workspaceId, LinkWithApiRequest, payload);
    }

    internal async Task ConnectWithApiOperation(Entity tag, OperationEntity apiOperation, string workspaceId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        var operationId = string.Format(IdString,
            azSubId, this.ExtractorParameters.ResourceGroup,
            this.ExtractorParameters.SourceApimName, workspaceId, $"apis/{apiOperation.ApiId}/operations", apiOperation.Id
            );
        var payload = new { Properties = new { operationId = operationId } };
        await this.Connect(tag, workspaceId, LinkWithApiOperationRequest, payload);
    }

    private async Task Connect(Entity tag, string workspaceId, string URL, object payload)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(URL,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspaceId, tag.DisplayName, Guid.NewGuid().ToString(), GlobalConstants.ApiVersion);
        
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = JsonContent.Create(payload, options: DefaultSerializerOptions);

        await this.CallApiManagementAsync(azToken, request);
    }
}
