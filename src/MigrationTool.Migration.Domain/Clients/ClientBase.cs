using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiVersionSet;
//using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiVersionSet;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients;

public class ClientBase : ApisClient
{
    const string GetVersionSetsRequest = "{0}{1}?api-version={2}";

    protected readonly IApiRevisionClient ApiRevisionClient;
    protected readonly ExtractorParameters ExtractorParameters;
    protected readonly IApiDataProcessor ApiDataProcessor;
    //protected readonly IApiVersionSetClient ApiVersionSetClient;
    readonly HttpClient HttpClient;

    protected readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ClientBase(IHttpClientFactory httpClientFactory,
        ExtractorParameters extractorParameters,
        IApiDataProcessor apiDataProcessor,
        //IApiVersionSetClient apiVersionSetClient,
        IApiRevisionClient apiRevisionClient)
        : base(httpClientFactory, apiDataProcessor)
    {
        //this.ApiVersionSetClient = apiVersionSetClient;
        this.ExtractorParameters = extractorParameters;
        this.HttpClient = httpClientFactory.CreateClient();
        this.ApiRevisionClient = apiRevisionClient;
        this.ApiDataProcessor = apiDataProcessor;
    }

    protected async Task<string> CallApiManagementAsync(String azToken, HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", azToken);
        HttpResponseMessage response = await this.HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string responseBody = await response.Content.ReadAsStringAsync();
        return responseBody;
    }

    protected async Task UploadPolicy(string requestUrl, string policy)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = new StringContent(policy, Encoding.UTF8, "application/vnd.ms-azure-apim.policy.raw+xml");
        await this.CallApiManagementAsync(azToken, request);
    }


    protected async Task<IReadOnlyCollection<Entity>> RemoveUnsupportedApis(List<ApiTemplateResource> apis)
    {
        List<Entity> toReturn = new List<Entity>();
        var versionedApis = apis
            .FindAll(api => api.Properties.ApiVersionSetId != null)
            .GroupBy(api => api.Properties.ApiVersionSetId).ToDictionary(
            group => group.Key,
            group => group.ToList()
            );

        foreach (var group in versionedApis)
        {
            var versionSetId = group.Value.First().Properties.ApiVersionSetId;

            var (azToken, azSubId) = await this.Auth.GetAccessToken();

            string requestUrl = string.Format(GetVersionSetsRequest,
               this.BaseUrl, versionSetId, GlobalConstants.ApiVersion);

            var apiVersionSetTemplateResource = await this.GetResponseAsync<ApiVersionSetTemplateResource>(azToken, requestUrl);

            var versionSet = new VersionSetEntity(apiVersionSetTemplateResource.Properties.DisplayName, apiVersionSetTemplateResource.Properties.DisplayName + $" (includes {group.Value.Count} versions)", apiVersionSetTemplateResource);

            versionSet.Apis = group.Value.ConvertAll(api =>
                new Entity(api.OriginalName, api.Properties.DisplayName, EntityType.Api, api));


            toReturn.Add(versionSet);
        }

        toReturn.AddRange(
            apis
                .FindAll(api => api.Properties.ApiVersionSetId == null)
                .ConvertAll(api => new Entity(api.OriginalName, api.Properties.DisplayName, EntityType.Api, api))
            );


        return toReturn;
    }
}