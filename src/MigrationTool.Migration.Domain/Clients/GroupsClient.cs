using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Groups;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using System.Net.Http.Json;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;

namespace MigrationTool.Migration.Domain.Clients;

public class GroupsClient : ClientBase, IGroupsClient
{
    const string CreateGroupRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/groups/{5}?api-version={6}";
    const string LinkWithProductRequest = 
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/products/{5}/groupLinks/{6}?api-version={7}";
    const string FetchUsersRequest = "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/groups/{4}/users?api-version={5}";
    const string AddUserRequest = "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/groups/{5}/users/{6}?api-version={7}";

    const string IdString = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.ApiManagement/service/{2}/workspaces/{3}/{4}/{5}";

    private class UserTemplateResource : TemplateResource
    {
        public string Name { get; set; }
    }

    private readonly IProductClient _productClient;
    public GroupsClient(IProductClient productClient, IHttpClientFactory httpClientFactory, ExtractorParameters extractorParameters, AzureCliAuthenticator authenticator = null): base(httpClientFactory, extractorParameters, authenticator)
    {
        this._productClient = productClient;
    }
    public async Task ConnectWithProduct(Entity group, Entity product, string workspaceId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        var groupId = string.Format(IdString,
            azSubId, this.ExtractorParameters.ResourceGroup,
            this.ExtractorParameters.SourceApimName, workspaceId, "groups", group.Id
            );
        var payload = new { Properties = new { groupId = groupId } };

        string requestUrl = string.Format(LinkWithProductRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspaceId, product.Id, Guid.NewGuid().ToString(), GlobalConstants.ApiVersion);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = JsonContent.Create(payload, options: DefaultSerializerOptions);

        await this.CallApiManagementAsync(azToken, request);
    }
    public async Task<Entity> Create(GroupTemplateResource resource, string workspaceId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateGroupRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspaceId, resource.Name, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = JsonContent.Create(resource, options: DefaultSerializerOptions);
        var response = await this.GetResponseBodyAsync(azToken, request);
        var armTemplate = response.Deserialize<GroupTemplateResource>();
        return new Entity(armTemplate.Name, EntityType.Group, armTemplate.Properties.DisplayName, armTemplate);
    }

    public async Task<IReadOnlyCollection<Entity>> FetchProducts(string groupId)
    {
        List<Entity> results = new();
        var products = await this._productClient.FetchAll();
        foreach (var product in products)
        {
            var groups = await this._productClient.FetchGroups(product.Id);
            if (groups.Where(group => group.Id.Equals(groupId)).Count() > 0)
            {
                results.Add(product);
            }
        }
        return results;
    }

    public async Task<IReadOnlyCollection<Entity>> FetchUsers(string groupId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(FetchUsersRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            groupId, GlobalConstants.ApiVersion);

        var users = await this.GetPagedResponseAsync<UserTemplateResource>(azToken, requestUrl);
        return users.ConvertAll(user => new Entity(user.Name, EntityType.User));
    }

    public async Task ConnectWithUser(Entity group, Entity user, string workspaceId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(AddUserRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspaceId, group.Id, user.Id, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        await this.CallApiManagementAsync(azToken, request);
    }
}

