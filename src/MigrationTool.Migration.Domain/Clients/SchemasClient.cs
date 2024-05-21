using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Schemas;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities;
using MigrationTool.Migration.Domain.Entities;
using System.Net.Http.Json;

namespace MigrationTool.Migration.Domain.Clients;

public class SchemasClient : ClientBase, Abstraction.ISchemasClient
{
    const string CreateSchemaRequest = 
        "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/schemas/{5}?api-version={6}";

    private readonly ISchemasClient schemasClient;
    private readonly ExtractorParameters extractorParamters;


    public SchemasClient(
        ExtractorParameters extractorParameters,
        ISchemasClient schemasClient,
        IHttpClientFactory httpClientFactory,
        AzureCliAuthenticator auth = null)
        : base(httpClientFactory, extractorParameters, auth)
    {
        this.extractorParamters = extractorParameters;
        this.schemasClient = schemasClient;
    }

    public async Task<Entity> Create(SchemaTemplateResource resource, string workspaceId)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreateSchemaRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspaceId, resource.Name, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = JsonContent.Create(resource, options: DefaultSerializerOptions);
        await this.CallApiManagementAsync(azToken, request);
        return new Entity(resource.Name, EntityType.Schema, resource.Name, resource);
    }

    public async Task<IReadOnlyCollection<Entity>> Fetch(IReadOnlyCollection<string> names) => 
        (await this.schemasClient.GetAllAsync(this.extractorParamters)).FindAll(g => names.Contains(g.Name))
        .ToList().ConvertAll(schema => new Entity(schema.Name, EntityType.Schema, schema.Name, schema));
}
