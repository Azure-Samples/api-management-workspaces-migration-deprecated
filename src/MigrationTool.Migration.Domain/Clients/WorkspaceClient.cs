using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;

namespace MigrationTool.Migration.Domain.Clients
{
    public class WorkspaceClient : ClientBase
    {
        const string GetWorkspacesRequest =
            "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces?api-version={4}";

        public WorkspaceClient(IHttpClientFactory httpClientFactory,
            ExtractorParameters extractorParameters,
            IApiDataProcessor apiDataProcessor,
            IApiRevisionClient apiRevisionClient)
            : base(httpClientFactory, extractorParameters, apiDataProcessor, apiRevisionClient)
        {
        }

        public async Task<IReadOnlyCollection<String>> FetchAll()
        {
            var (azToken, azSubId) = await this.Auth.GetAccessToken();
            string requestUrl = string.Format(GetWorkspacesRequest,
                this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
                GlobalConstants.ApiVersion);

            var response = await this.GetPagedResponseAsync<TemplateResource>(azToken, requestUrl);
            return response.ConvertAll(entry => entry.Name);
        }
    }
}