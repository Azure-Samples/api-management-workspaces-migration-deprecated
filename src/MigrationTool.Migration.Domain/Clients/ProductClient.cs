using System.Net.Http.Json;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ProductApis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;
using Newtonsoft.Json;

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

        const string AddApiWorkspaceLevelRequest =
            "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/products/{5}/apiLinks/{6}?api-version={7}";

        const string AddApiServiceLevelRequest =
            "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/products/{4}/apiLinks/{5}?api-version={6}";

        const string GetAllApisLinkedToProductRequest =
            "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/products/{4}/apis?api-version={5}&$filter=isCurrent";

        const string AddTagRequest =
            "{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.ApiManagement/service/{3}/workspaces/{4}/products/{5}/tags/{6}?api-version={7}";

        private readonly IApisClient ApisClient;
        private readonly IPolicyClient PolicyClient;
        private readonly IApiRevisionClient ApiRevisionClient;
        private readonly IApiDataProcessor ApiDataProcessor;
        private readonly ITagClient TagClient;
        private readonly IProductsClient ProductsClient;

        public ProductClient(ExtractorParameters extractorParameters,
            IApisClient apisClient,
            IPolicyClient policyClient,
            IHttpClientFactory httpClientFactory,
            IApiRevisionClient apiRevisionClient,
            IApiDataProcessor apiDataProcessor,
            IProductsClient productsClient,
            ITagClient tagClient)
            : base(httpClientFactory, extractorParameters)
        {
            this.ApisClient = apisClient;
            this.PolicyClient = policyClient;
            this.ApiRevisionClient = apiRevisionClient;
            this.ApiDataProcessor = apiDataProcessor;
            this.TagClient = tagClient;
            this.ProductsClient = productsClient;
        }

        public async Task<IReadOnlyCollection<Entity>> FetchApis(string entityId)
        {
            var allApis = await this.ApisClient.GetAllAsync(this.ExtractorParameters);
            var processedApis = this.CreateApiEntities(allApis);
            
            var productApis = await this.ApisClient.GetAllLinkedToProductAsync(entityId, this.ExtractorParameters);
            var productApisSet = productApis.Select(api => api.OriginalName).ToHashSet();
            return processedApis.Where(api => productApisSet.Contains(api.Id)).ToList();
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
            var response = await this.GetResponseBodyAsync(azToken, request);
            var armTemplate = response.Deserialize<ProductApiTemplateResource>();
            return new Entity(armTemplate.Name, EntityType.Product, armTemplate.Properties.DisplayName, armTemplate);
        }

        public async Task<IReadOnlyCollection<Entity>> FetchTags(string entityId)
        {
            var tags = await this.TagClient.GetAllTagsLinkedToProductAsync(entityId, this.ExtractorParameters);
            return tags.ConvertAll(tag =>
                new Entity(tag.Properties.DisplayName, EntityType.Tag, tag.Properties.DisplayName, tag));
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

        public async Task AddApi(Entity product, Entity api, string productWorkspace, string apiWorkspace)
        {
            var (azToken, azSubId) = await this.Auth.GetAccessToken();
            string requestUrl;
            if (productWorkspace.IsNullOrEmpty())
            {
                requestUrl = string.Format(AddApiServiceLevelRequest,
                    this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
                    product.Id, Guid.NewGuid(), GlobalConstants.ApiVersion);
            }
            else
            {
                requestUrl = string.Format(AddApiWorkspaceLevelRequest,
                    this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
                    productWorkspace, product.Id, Guid.NewGuid(), GlobalConstants.ApiVersion);
            }
            dynamic obj = CreateLinkObject(api, apiWorkspace);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
            request.Content = JsonContent.Create(obj, options: DefaultSerializerOptions);
            await this.CallApiManagementAsync(azToken, request);
        }

        public async Task<Entity> Fetch(string id)
        {
            var products = await this.ProductsClient.GetAllAsync(this.ExtractorParameters);
            var product = products.Where(product => product.Name.Equals(id)).First();
            var productCoverted = JsonConvert.DeserializeObject<ProductApiTemplateResource>(JsonConvert.SerializeObject(product));
            
            return new Entity(product.Name, EntityType.Product, product.Properties.DisplayName, productCoverted);
        }

        internal async Task AddTag(Entity product, Entity tag, string workspace)
        {
            var (azToken, azSubId) = await this.Auth.GetAccessToken();
            string requestUrl = string.Format(AddTagRequest,
                    this.BaseUrl, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName,
                    workspace, product.Id, tag.DisplayName, GlobalConstants.ApiVersion);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
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