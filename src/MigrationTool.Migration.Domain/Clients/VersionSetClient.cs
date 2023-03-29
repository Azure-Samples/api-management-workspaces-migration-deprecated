
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

    const string WorkspaceIdFormat = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.ApiManagement/service/{2}/workspaces/{3}/apiVersionSets/{4}";

    private readonly ApiClient ApiClient;

    public VersionSetClient(
        ExtractorParameters extractorParameters,
        ApiClient apiClient, 
        IApiRevisionClient apiRevisionClient,
        IHttpClientFactory httpClientFactory,
        IApiDataProcessor apiDataProcessor
        )
        : base(httpClientFactory, extractorParameters, apiDataProcessor, apiRevisionClient)
    {
        this.ApiClient = apiClient;
    }

    public async Task<Entity?> FetchVersionSet(Entity api)
    {
        var apis = await this.ApiClient.FetchAllApisAndVersionSets();
        return apis.Where(api => api.Type == EntityType.VersionSet).Where(versionSet => versionSet.Id == ((ApiTemplateResource)api.ArmTemplate).Properties.ApiVersionSetId).FirstOrDefault();
    }




    public async Task<Entity> Create(Entity sourceEntity, Func<string, string> modifier, string workspace)
    {
        return await CreateOrUpdateApi(sourceEntity, modifier, modifier(sourceEntity.ArmTemplate.Name), workspace);
    }

    public async Task<Entity> Update(Entity sourceEntity, Func<string, string> modifier, string workspace)
    {
        return await CreateOrUpdateApi(sourceEntity, modifier, sourceEntity.ArmTemplate.Name, workspace);
    }

    private async Task<Entity> CreateOrUpdateApi(Entity sourceEntity, Func<string, string> modifier, string newId,
        string workspace)
    {
        if (sourceEntity.Type != EntityType.VersionSet)
            throw new ArgumentException("Provided entity should be of type VersionSet");

        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        var newFullId = string.Format(WorkspaceIdFormat,
            azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspace, newId);
        string requestUrl = string.Format(CreateVersionSetRequest,
            this.BaseUrl, newFullId, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);

        var versionSetTemplate = ((ApiVersionSetTemplateResource)sourceEntity.ArmTemplate).Copy();
        versionSetTemplate.Name = null;
        versionSetTemplate.Properties.DisplayName = modifier(versionSetTemplate.Properties.DisplayName);

        request.Content = JsonContent.Create<ApiVersionSetTemplateResource>(versionSetTemplate, options: DefaultSerializerOptions);
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
