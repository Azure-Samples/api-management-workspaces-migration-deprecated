using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiVersionSet;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients;

public class ClientBase : ApiClientBase
{
    const string GetVersionSetsRequest = "{0}{1}?api-version={2}";

    protected readonly IApiRevisionClient ApiRevisionClient;
    protected readonly ExtractorParameters ExtractorParameters;
    protected readonly HttpClient HttpClient;

    protected readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ClientBase(IHttpClientFactory httpClientFactory, ExtractorParameters extractorParameters)
        : base(httpClientFactory)
    {
        this.ExtractorParameters = extractorParameters;
        this.HttpClient = httpClientFactory.CreateClient();
    }

    protected async Task<string> GetResponseBodyAsync(String azToken, HttpRequestMessage request)
    {
        var response = await this.CallApiManagementAsync(azToken, request);

        string responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }

    protected async Task<HttpResponseMessage> CallApiManagementAsync(String azToken, HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", azToken);
        HttpResponseMessage response = await this.HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return response;
    }


    protected async Task UploadPolicy(string requestUrl, string policy)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = new StringContent(policy, Encoding.UTF8, "application/vnd.ms-azure-apim.policy.raw+xml");
        await this.CallApiManagementAsync(azToken, request);
    }


    protected async Task<IReadOnlyCollection<Entity>> ProcessApiData(List<ApiTemplateResource> apis)
    {
        var apisWithRevisions = this.CreateApiEntities(apis);
        var apisWithoutVersionSet = this.ApisWithoutVersionSet(apisWithRevisions);
        var versionSets = await this.CreateVersionSets(apisWithRevisions);

        List<Entity> processed = new List<Entity>();
        processed.AddRange(versionSets);
        processed.AddRange(apisWithoutVersionSet);
        return processed;
    }

    List<ApiEntity> ApisWithoutVersionSet(List<ApiEntity> apisWithRevisions) =>
        apisWithRevisions.FindAll(api => api.ArmTemplate.Properties.ApiVersionSetId == null);

    async Task<List<VersionSetEntity>> CreateVersionSets(List<ApiEntity> apisWithRevisions)
    {
        List<VersionSetEntity> processed = new List<VersionSetEntity>();
        var versionedApis = apisWithRevisions
            .FindAll(api => api.ArmTemplate.Properties.ApiVersionSetId != null)
            .GroupBy(api => api.ArmTemplate.Properties.ApiVersionSetId)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var group in versionedApis)
        {
            var versionSetId = group.Value.First().ArmTemplate.Properties.ApiVersionSetId;
            var (azToken, azSubId) = await this.Auth.GetAccessToken();

            string requestUrl = string.Format(GetVersionSetsRequest,
                this.BaseUrl, versionSetId, GlobalConstants.ApiVersion);

            var apiVersionSetTemplateResource =
                await this.GetResponseAsync<ApiVersionSetTemplateResource>(azToken, requestUrl);
            var versionSet = new VersionSetEntity(versionSetId, apiVersionSetTemplateResource.Properties.DisplayName,
                apiVersionSetTemplateResource);

            versionSet.Apis = group.Value;

            processed.Add(versionSet);
        }

        return processed;
    }

    protected List<ApiEntity> CreateApiEntities(List<ApiTemplateResource> apis)
    {
        List<ApiEntity> apisWithRevisions = new List<ApiEntity>();
        var regex = new Regex("^(.*);rev=.+$");
        var revisionGroups = apis.GroupBy(api => regex.Match(api.Name).Groups[1].Value)
            .ToDictionary(g => g.FirstOrDefault(api => !api.OriginalName.Contains(";rev="), g.First()),
                g => g.Where(api => api.OriginalName.Contains(";rev=")).ToList());
        foreach (var revisionGroup in revisionGroups)
        {
            var api = revisionGroup.Key;
            var apiEntity = new ApiEntity(api.OriginalName, api.Properties.DisplayName, api);
            apiEntity.Revisions =
                revisionGroup.Value.ConvertAll(a => new ApiEntity(a.Name, a.Properties.DisplayName, a));
            apisWithRevisions.Add(apiEntity);
        }

        return apisWithRevisions;
    }
}