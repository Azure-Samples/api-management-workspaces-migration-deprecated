using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.TemplateModels;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.NamedValues;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients;

public class NamedValuesClient : ClientBase
{
    private const string NamedValueRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/namedValues/{4}?api-version={5}";

    private const string CreateRequest =
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/namedValues/{5}?api-version={6}";

    private readonly INamedValuesClient namedValuesClient;

    public NamedValuesClient(IHttpClientFactory httpClientFactory,
        ExtractorParameters extractorParameters,
        INamedValuesClient namedValuesClient)
        : base(httpClientFactory, extractorParameters)
    {
        this.namedValuesClient = namedValuesClient;
    }

    public async Task<IReadOnlyCollection<Entity>> Fetch(IReadOnlyCollection<string> namedValuesNames)
    {
        var set = namedValuesNames.ToHashSet();
        var namedValues = await this.namedValuesClient.GetAllAsync(this.ExtractorParameters);
        return namedValues.Where(r => set.Contains(r.Properties.DisplayName))
            .Select(_ => new Entity(_.Name, EntityType.NamedValue, _.Properties.DisplayName, _))
            .ToList();
    }

    public async Task<IReadOnlyCollection<string>> FetchReferenceIds(string id)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(NamedValueRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            id, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);


        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", azToken);
        HttpResponseMessage response = await this.HttpClient.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var body = responseBody.Deserialize<ErrorResponseBody>();
            return body.Error.Message
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .ToList();
        }

        response.EnsureSuccessStatusCode();

        // TODO recreate if was removed

        return new List<string>();
    }

    public async Task Create(NamedValueTemplateResource resource, string workspaceId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspaceId, resource.Name, GlobalConstants.ApiVersion);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = JsonContent.Create(resource, options: DefaultSerializerOptions);

        await this.CallApiManagementAsync(azToken, request);
    }
}

class ErrorResponseBody
{
    public ErrorProperties Error { get; set; }
}

class ErrorProperties
{
    public string Message { get; set; }
}