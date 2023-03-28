using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients;

public class ClientBase : ApisClient
{
    protected readonly IApiRevisionClient ApiRevisionClient;
    protected readonly ExtractorParameters ExtractorParameters;
    protected readonly IApiDataProcessor ApiDataProcessor;
    readonly HttpClient HttpClient;

    protected readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ClientBase(IHttpClientFactory httpClientFactory,
        ExtractorParameters extractorParameters,
        IApiDataProcessor apiDataProcessor,
        IApiRevisionClient apiRevisionClient)
        : base(httpClientFactory, apiDataProcessor)
    {
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
        apis = apis.FindAll(api => api.Properties.ApiVersionSetId == null); //remove apis with versions
        List<ApiTemplateResource> apisWithoutRevisions = new List<ApiTemplateResource>();
        foreach (var api in apis)
        {
            var apiRevisions =
                await this.ApiRevisionClient.GetApiRevisionsAsync(api.OriginalName, this.ExtractorParameters);
            if (apiRevisions.Count == 1)
            {
                apisWithoutRevisions.Add(api);
            }
        }

        return apisWithoutRevisions.ConvertAll(api =>
            new Entity(api.OriginalName, EntityType.Api, api.Properties.DisplayName, api));
    }
}