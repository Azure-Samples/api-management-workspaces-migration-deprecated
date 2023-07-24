//using ARM = Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Groups;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using System.Net.Http.Json;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities;
using System;
using YamlDotNet.Core.Tokens;
using System.Security.Policy;

namespace MigrationTool.Migration.Domain.Clients;

public class GroupsClient : ClientBase, IGroupsClient
{
    const string CreateGroupRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/groups/{5}?api-version={6}";
    const string LinkWithProductRequest = 
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/products/{5}/groupLinks/{6}?api-version={7}";
    const string IdString = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.ApiManagement/service/{2}/workspaces/{3}/{4}/{5}";

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
            workspaceId, product.DisplayName, Guid.NewGuid().ToString(), GlobalConstants.ApiVersion);

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

    public async Task<IReadOnlyCollection<Entity>> FetchEntities(string groupId)
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
}
