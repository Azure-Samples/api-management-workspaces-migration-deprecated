using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.PolicyFragments;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ProductApis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using System.Net.Http;
using System.Net.Http.Json;

namespace MigrationTool.Migration.Domain.Clients;

public class PolicyFragmentClient : ClientBase, IPolicyFragmentClient
{
    const string CreatePolicyFragmentRequest =
    "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/policyFragments/{5}?api-version={6}";

    private readonly IPolicyFragmentsClient policyFragmentsClient;
    private readonly ExtractorParameters extractorParamters;


    public PolicyFragmentClient(
        ExtractorParameters extractorParameters,
        IPolicyFragmentsClient policyFragmentsClient,
        IHttpClientFactory httpClientFactory,
        AzureCliAuthenticator auth = null)
        : base(httpClientFactory, extractorParameters, auth)
    {
        this.extractorParamters = extractorParameters;
        this.policyFragmentsClient = policyFragmentsClient;
    }

    public async Task<IReadOnlyCollection<Entity>> Fetch(IReadOnlyCollection<string> policyFragmentsNames)
    {
        var fragments = await this.policyFragmentsClient.GetAllAsync(this.extractorParamters);
        return fragments.FindAll(g => policyFragmentsNames.Contains(g.Name)).ToList().ConvertAll(fragment => new Entity(fragment.Name, EntityType.PolicyFragment, fragment.Name, fragment));
    }

    public async Task<Entity> Create(PolicyFragmentsResource resource, string workspace)
    {
        var (azToken, azSubId) = await this.Auth.GetAccessToken();
        string requestUrl = string.Format(CreatePolicyFragmentRequest,
            this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
            workspace, resource.Name, GlobalConstants.ApiVersion);
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
        request.Content = JsonContent.Create(resource, options: DefaultSerializerOptions);
        await this.CallApiManagementAsync(azToken, request);
        return new Entity(resource.Name, EntityType.PolicyFragment, resource.Name, resource);
    }

}