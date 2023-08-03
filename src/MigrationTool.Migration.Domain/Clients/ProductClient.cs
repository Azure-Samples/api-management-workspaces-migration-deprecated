using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ProductApis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using Newtonsoft.Json;
using ARM = Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;

namespace MigrationTool.Migration.Domain.Clients
{
    public class ProductClient : ClientBase, IProductClient
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
        private readonly ARM.ITagClient TagClient;
        private readonly ARM.IGroupsClient GroupsClient;
        private readonly IProductsClient ProductsClient;

        public ProductClient(ExtractorParameters extractorParameters,
            IApisClient apisClient,
            IPolicyClient policyClient,
            IHttpClientFactory httpClientFactory,
            IApiRevisionClient apiRevisionClient,
            IApiDataProcessor apiDataProcessor,
            IProductsClient productsClient,
            ARM.ITagClient tagClient,
            ARM.IGroupsClient groupsClient,
            AzureCliAuthenticator auth = null)
            : base(httpClientFactory, extractorParameters, auth)
        {
            this.ApisClient = apisClient;
            this.PolicyClient = policyClient;
            this.ApiRevisionClient = apiRevisionClient;
            this.ApiDataProcessor = apiDataProcessor;
            this.TagClient = tagClient;
            this.ProductsClient = productsClient;
            this.GroupsClient = groupsClient;
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
                new Entity(tag.Name, EntityType.Tag, tag.Properties.DisplayName, tag));
        }

        public async Task<IReadOnlyCollection<Entity>> FetchGroups(string entityId)
        {
            var groups = await this.GroupsClient.GetAllLinkedToProductAsync(entityId, this.ExtractorParameters);
            return groups.FindAll(g => g.Name != "administrators").ToList().ConvertAll(group => new Entity(group.Name, EntityType.Group, group.Properties.DisplayName, group));
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
            dynamic obj = CreateLinkObject(api, apiWorkspace, azSubId, this.ExtractorParameters.ResourceGroup, this.ExtractorParameters.SourceApimName);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
            request.Content = JsonContent.Create(obj, options: DefaultSerializerOptions);
            await this.CallApiManagementAsync(azToken, request);
        }

        public async Task<Entity> Fetch(string id)
        {
            var products = await this.FetchAll();
            return products.Where(product => product.Id.Equals(id)).First();
        }

        static dynamic CreateLinkObject(Entity api, string workspace, string subscription, string resourceGroup, string service) =>
            new
            {
                properties = new
                {
                    apiId = $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ApiManagement/service/{service}/workspaces/{workspace}/apis/{api.Id}"
                }
            };

        public async Task<IReadOnlyCollection<Entity>> FetchAll()
        {
            var products = await this.ProductsClient.GetAllAsync(this.ExtractorParameters);
            return products.ConvertAll(product => new Entity(product.Name, EntityType.Product, product.Properties.DisplayName, product));
        }
    }
}