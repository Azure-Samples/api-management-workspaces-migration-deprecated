using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiVersionSet;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;
using System.Net.Http.Json;

namespace MigrationTool.Migration.Domain.Clients;

public class VersionSetClient : ClientBase
{
    const string CreateVersionSetRequest =
        "{0}{1}/?api-version={2}";

    const string WorkspaceIdFormat =
        "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.ApiManagement/service/{2}/workspaces/{3}/apiVersionSets/{4}";

    private readonly ApiClient ApiClient;

    public VersionSetClient(
        ExtractorParameters extractorParameters,
        ApiClient apiClient, 
        IApiRevisionClient apiRevisionClient,
        IHttpClientFactory httpClientFactory,
        IApiDataProcessor apiDataProcessor
        )
        : base(httpClientFactory, extractorParameters)
    {
        this.ApiClient = apiClient;
    }

    public async Task<Entity?> FetchVersionSet(Entity api)
    {
        var apis = await this.ApiClient.FetchAllApisAndVersionSets();
        return apis
            .Where(api => api.Type == EntityType.VersionSet)
            .FirstOrDefault(versionSet => versionSet.Id == ((ApiTemplateResource)api.ArmTemplate).Properties.ApiVersionSetId);
    }


    public async Task<Entity> Create(ApiVersionSetTemplateResource versionSetTemplate, string workspace)
    {
        return await CreateOrUpdateApi(versionSetTemplate, workspace);
    }

    public async Task<Entity> Update(ApiVersionSetTemplateResource versionSetTemplate, string workspace)
    {
        return await CreateOrUpdateApi(versionSetTemplate, workspace);
    }

    private async Task<Entity> CreateOrUpdateApi(ApiVersionSetTemplateResource versionSetTemplate, string workspace)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        var newFullId = string.Format(WorkspaceIdFormat,
            azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspace, versionSetTemplate.Name);
        string requestUrl = string.Format(CreateVersionSetRequest,
            this.BaseUrl, newFullId, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = JsonContent.Create(versionSetTemplate, options: DefaultSerializerOptions);
        var response = await this.CallApiManagementAsync(azToken, request);
        var armTemplate = response.Deserialize<ApiVersionSetTemplateResource>();
        return new VersionSetEntity(newFullId, armTemplate.Properties.DisplayName, armTemplate);
    }


    public async Task Delete(Entity api)
    {
        if (api.Type != EntityType.VersionSet)
            throw new ArgumentException("Provided entity should be of type VersionSet");

        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateVersionSetRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            api.Id, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
        await this.CallApiManagementAsync(azToken, request);
    }
}