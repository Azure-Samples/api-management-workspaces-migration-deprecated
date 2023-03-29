using System.Net.Http.Json;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ProductApis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;

namespace MigrationTool.Migration.Domain.Clients
{
    public class ProductClient : ClientBase
    {
        const string CreateProductPolicyRequest =
            "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/products/{5}/policies/policy?api-version={6}";

        const string CreateProductRequest =
            "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/products/{5}?api-version={6}";

        const string DeleteProductRequest =
            "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/products/{4}?api-version={5}";

        const string AddApiRequest =
            "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/products/{5}/apiLinks/{6}?api-version={7}";

        const string GetAllApisLinkedToProductRequest =
            "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/products/{4}/apis?api-version={5}&$filter=isCurrent";

        private readonly IApisClient ApisClient;
        private readonly IPolicyClient PolicyClient;
        private readonly IApiRevisionClient ApiRevisionClient;
        private readonly IApiDataProcessor ApiDataProcessor;

        public ProductClient(ExtractorParameters extractorParameters,
            IApisClient apisClient,
            IPolicyClient policyClient,
            IHttpClientFactory httpClientFactory,
            IApiRevisionClient apiRevisionClient,
            IApiDataProcessor apiDataProcessor)
            : base(httpClientFactory, extractorParameters)
        {
            this.ApisClient = apisClient;
            this.PolicyClient = policyClient;
            this.ApiRevisionClient = apiRevisionClient;
            this.ApiDataProcessor = apiDataProcessor;
        }

        public async Task<IReadOnlyCollection<Entity>> FetchApis(string entityId)
        {
            var (azToken, azSubId) = await this.Auth.GetAccessToken();
            string requestUrl = string.Format(GetAllApisLinkedToProductRequest,
                this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
                entityId, GlobalConstants.ApiVersion);
            var apis = await this.GetPagedResponseAsync<ApiTemplateResource>(azToken, requestUrl);
            this.ApiDataProcessor.ProcessData(apis);


            return await this.ProcessApiData(apis);
        }

        public async Task<string?> FetchPolicy(string entityId)
        {
            var policy = await this.PolicyClient.GetPolicyLinkedToProductAsync(entityId, this.ExtractorParameters);
            return policy?.Properties?.PolicyContent;
        }

        public async Task<Entity> CreateProduct(ProductApiTemplateResource resource, string workspace) =>
            await this.CreateOrUpdateProduct(resource, workspace);

        public async Task<Entity> UpdateProduct(ProductApiTemplateResource resource, string workspace) =>
            await this.CreateOrUpdateProduct(resource, workspace);

        private async Task<Entity> CreateOrUpdateProduct(ProductApiTemplateResource resource, string workspace)
        {
            var (azToken, azSubId) = await this.Auth.GetAccessToken();
            string requestUrl = string.Format(CreateProductRequest,
                this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
                workspace, resource.Name, GlobalConstants.ApiVersion);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
            request.Content = JsonContent.Create(resource, options: DefaultSerializerOptions);
            var response = await this.CallApiManagementAsync(azToken, request);
            var armTemplate = response.Deserialize<ProductApiTemplateResource>();
            return new Entity(armTemplate.Name, EntityType.Product, armTemplate.Properties.DisplayName, armTemplate);
        }

        public Task<IReadOnlyCollection<Entity>> FetchTags(string entityId)
        {
            // TODO: Implement
            return Task.FromResult<IReadOnlyCollection<Entity>>(new List<Entity>());
        }

        public Task<IReadOnlyCollection<Entity>> FetchGroups(string entityId)
        {
            // TODO: Implement
            return Task.FromResult<IReadOnlyCollection<Entity>>(new List<Entity>());
        }

        public async Task UploadProductPolicy(Entity product, string policy, string workspace)
        {
            if (product.Type != EntityType.Product)
                throw new ArgumentException("Provided entity should be of type Product");

            var (azToken, azSubId) = await this.Auth.GetAccessToken();
            string requestUrl = string.Format(CreateProductPolicyRequest,
                this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
                workspace, product.Id, GlobalConstants.ApiVersion);

            await UploadPolicy(requestUrl, policy);
        }

        public async Task DeleteProduct(Entity product)
        {
            if (product.Type != EntityType.Product)
                throw new ArgumentException("Provided entity should be of type Product");

            var (azToken, azSubId) = await this.Auth.GetAccessToken();
            string requestUrl = string.Format(DeleteProductRequest,
                this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
                product.Id, GlobalConstants.ApiVersion);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
            await this.CallApiManagementAsync(azToken, request);
        }

        public async Task AddApi(Entity product, Entity api, string workspace)
        {
            var (azToken, azSubId) = await this.Auth.GetAccessToken();
            string requestUrl = string.Format(AddApiRequest,
                this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
                workspace, product.Id, Guid.NewGuid(), GlobalConstants.ApiVersion);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
            dynamic obj = CreateLinkObject(api, workspace);
            request.Content = JsonContent.Create(obj, options: DefaultSerializerOptions);
            await this.CallApiManagementAsync(azToken, request);
        }

        static dynamic CreateLinkObject(Entity api, string workspace) =>
            new
            {
                properties = new
                {
                    apiId = $"/workspaces/{workspace}/apis/{api.Id}"
                }
            };
    }
}