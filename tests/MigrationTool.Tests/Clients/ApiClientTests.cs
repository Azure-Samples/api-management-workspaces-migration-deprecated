using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Executor.Operations;
using Moq.Contrib.HttpClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MigrationTool.Migration.Domain.Entities;
using Moq.Protected;
using Moq;
using System.Net;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Products;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ProductApis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Policy;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiOperations;
using MigrationTool.Tests.Helpers;
using System.Collections;
using Newtonsoft.Json;
using FluentAssertions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Tags;

namespace MigrationTool.Tests.Clients;

[TestClass]
public class ApiClientTests : BaseTest
{
    private ApiClient client;
    HttpRequestMessage request = null;

    [TestInitialize]
    public void Initialize()
    {
        this.client = new ApiClient(this.extractorParameters, this.apisClient.Object, this.productsClient.Object, this.apiOperationClient.Object, this.policyClient.Object, this.httpHandler.CreateClientFactory(), this.entitiesRegistry, this.tagClient.Object, this.auth.Object);

        this.httpHandler.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK)
            .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });
    }
    [TestMethod]
    public async Task UploadPolicy()
    {
        //arrange
        var entity = new Entity("1", EntityType.Api);
        var policy = "some-policy";
        var workspace = "workspace-id";

        //act
        await this.client.UploadApiPolicy(entity, policy, workspace);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().EndWith($"/workspaces/{workspace}/apis/{entity.Id}/policies/policy?api-version=2022-09-01-preview");
        var body = await request.Content.ReadAsStringAsync();
        Assert.AreEqual(body, policy);
    }

    [TestMethod]
    public async Task UploadApiOperationPolicy()
    {
        //arrange
        var apiId = "1";
        var policy = "some operation policy";
        var workspace = "workspace-id";
        var operationId = "10";

        //act
        await this.client.UploadApiOperationPolicy(apiId, operationId, policy, workspace);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().EndWith($"/workspaces/{workspace}/apis/{apiId}/operations/{operationId}/policies/policy?api-version=2022-09-01-preview");
        var body = await request.Content.ReadAsStringAsync();
        Assert.AreEqual(body, policy);
    }

    [TestMethod]
    public async Task Delete()
    {
        //arrange
        var entity = new Entity("1", EntityType.Api);

        //act
        await this.client.Delete(entity);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Delete);
        request.RequestUri.PathAndQuery.Should().EndWith($"/apis/{entity.Id}?api-version=2022-09-01-preview");
        Assert.IsNull(request.Content);
    }

    [TestMethod]
    public async Task FetchAllApisAndVersionSetsReturnsEmptyListWhenThereIsNoAPIs()
    {
        //arrange
        List<ApiTemplateResource> list = new();
        this.apisClient.Setup(client => client.GetAllAsync(It.IsAny<ExtractorParameters>(), It.IsAny<bool>()).Result).Returns(list);

        //act
        var result = await this.client.FetchAllApisAndVersionSets();

        //verify
        Assert.AreEqual(result.LongCount(), 0);
    }

    [TestMethod]
    public async Task FetchAllApisAndVersionSetsDifferentApis()
    {
        //arrange
        List<ApiTemplateResource> list = new();
        list.Add(new ApiTemplateResource() { Name = "test-api-1;rev=1", OriginalName = "test-original-name-1", Properties = new ApiProperties { DisplayName = "test-api-dn-1" } });
        list.Add(new ApiTemplateResource() { Name = "test-api-2;rev=1", OriginalName = "test-original-name-2", Properties = new ApiProperties { DisplayName = "test-api-dn-2" } });

        this.apisClient.Setup(client => client.GetAllAsync(It.IsAny<ExtractorParameters>(), It.IsAny<bool>()).Result).Returns(list);

        //act
        var result = await this.client.FetchAllApisAndVersionSets();

        //verify
        Assert.AreEqual(result.LongCount(), 2);
        NUnit.Framework.Assert.That(result.Select(item => (item as ApiEntity).Revisions.Count), NUnit.Framework.Is.All.EqualTo(0));
        CollectionAssert.AreEqual(result.Select(item => item.ArmTemplate).ToList(), list, this.comparer); //check if both apis where imported correctly
    }

    [TestMethod]
    public async Task FetchAllApisAndVersionSetsAPIWithRevisions()
    {
        //arrange
        List<ApiTemplateResource> list = new();
        list.Add(new ApiTemplateResource() { Name = "test-api-1;rev=1", OriginalName = "test-original-name-1", Properties = new ApiProperties { DisplayName = "test-api-dn-1" } });
        list.Add(new ApiTemplateResource() { Name = "test-api-1;rev=2", OriginalName = "test-original-name-1;rev=2", Properties = new ApiProperties { DisplayName = "test-api-dn-1" } });
        list.Add(new ApiTemplateResource() { Name = "test-api-1;rev=3", OriginalName = "test-original-name-1;rev=3", Properties = new ApiProperties { DisplayName = "test-api-dn-1" } });

        this.apisClient.Setup(client => client.GetAllAsync(It.IsAny<ExtractorParameters>(), It.IsAny<bool>()).Result).Returns(list);

        //act
        var result = await this.client.FetchAllApisAndVersionSets();

        //verify
        Assert.AreEqual(result.LongCount(), 1);
        Assert.AreEqual(result.ToList()[0].ArmTemplate, list[0], "first revision should be saved as api entity.");
        CollectionAssert.AreEqual((result.ToList()[0] as ApiEntity).Revisions.Select(rev => rev.ArmTemplate).ToList(), list.GetRange(1, 2), this.comparer, "n+1 revision should be saved under the Revisions field.");
    }

    [TestMethod]
    public async Task FetchAllApisAndVersionSetsCollectsAPIsUnderVersionSetObject()
    {
        //arrange
        var versionSetId = "some-versionset";
        string payload = $"{{ \"ApiVersion\":null,\"Type\":\"Microsoft.ApiManagement/service/apiVersionSets\",\"Name\":\"{versionSetId}\",\"Scale\":null,\"DependsOn\":null,\"Properties\":{{ \"DisplayName\":\"abcd\",\"Description\":null,\"VersionQueryName\":null,\"VersionHeaderName\":null,\"VersioningScheme\":\"Segment\"}} }}";
        List<ApiTemplateResource> list = new();
        list.Add(new ApiTemplateResource() { Name = "test-api-1;rev=1", OriginalName = "test-original-name-1", Properties = new ApiProperties { DisplayName = "test-api-dn-1", ApiVersionSetId = "/" + versionSetId } });
        list.Add(new ApiTemplateResource() { Name = "test-api-2;rev=1", OriginalName = "test-original-name-2", Properties = new ApiProperties { DisplayName = "test-api-dn-2", ApiVersionSetId = "/" + versionSetId } });

        this.httpHandler.SetupRequest($"https://management.azure.com/{versionSetId}?api-version=2022-09-01-preview")
            .ReturnsResponse(HttpStatusCode.OK, new StringContent(payload, Encoding.UTF8, "application/json"));

        this.apisClient.Setup(client => client.GetAllAsync(It.IsAny<ExtractorParameters>(), It.IsAny<bool>()).Result).Returns(list);

        //act
        var result = await this.client.FetchAllApisAndVersionSets();

        //verify
        Assert.AreEqual(result.LongCount(), 1);
        CollectionAssert.AreEqual((result.ToList()[0] as VersionSetEntity).Apis.Select(api => api.ArmTemplate).ToList(), list, this.comparer);
    }

    [TestMethod]
    public async Task FetchAllApisFlatFetchesVersionedApisAsNormalOnes()
    {
        //arrange
        var versionSetId = "some-versionset";
        List<ApiTemplateResource> list = new();
        list.Add(new ApiTemplateResource() { Name = "test-api-1;rev=1", OriginalName = "test-original-name-1", Properties = new ApiProperties { DisplayName = "test-api-dn-1", ApiVersionSetId = "/" + versionSetId } });
        list.Add(new ApiTemplateResource() { Name = "test-api-2;rev=1", OriginalName = "test-original-name-2", Properties = new ApiProperties { DisplayName = "test-api-dn-2", ApiVersionSetId = "/" + versionSetId } });
        
        list.Add(new ApiTemplateResource() { Name = "test-api-3;rev=1", OriginalName = "test-original-name-3", Properties = new ApiProperties { DisplayName = "test-api-dn-3" } });
        list.Add(new ApiTemplateResource() { Name = "test-api-3;rev=2", OriginalName = "test-original-name-3;rev=2", Properties = new ApiProperties { DisplayName = "test-api-dn-3" } });
        list.Add(new ApiTemplateResource() { Name = "test-api-3;rev=3", OriginalName = "test-original-name-3;rev=3", Properties = new ApiProperties { DisplayName = "test-api-dn-3" } });

        this.apisClient.Setup(client => client.GetAllAsync(It.IsAny<ExtractorParameters>(), It.IsAny<bool>()).Result).Returns(list);

        //act
        var result = await this.client.FetchAllApisFlat();

        //verify
        Assert.AreEqual(result.LongCount(), 3);
        CollectionAssert.AreEqual(result.ToList().GetRange(0, 3).Select(api => api.ArmTemplate).ToList(), list.GetRange(0, 3), this.comparer, "first 3 elements should be listed as APIs");
        CollectionAssert.AreEqual((result.ToList()[2] as ApiEntity).Revisions.Select(rev => rev.ArmTemplate).ToList(), list.GetRange(3, 2), this.comparer, "revisions should be saved under the Revisions field.");
    }

    [TestMethod]
    public async Task FetchProducts()
    {
        //arange
        var id = "some-id";
        List<ProductApiTemplateResource> list = new();
        list.Add(new ProductApiTemplateResource() { Name="test-product", Properties = new ProductApiProperties() { Description = "description", DisplayName = "test-product", SubscriptionRequired = true } });
        list.Add(new ProductApiTemplateResource() { Name = "test-product2", Properties = new ProductApiProperties() { Description = "description2", DisplayName = "test-product2", SubscriptionRequired = true } });
        this.productsClient.Setup(client => client.GetAllLinkedToApiAsync(id, It.IsAny<ExtractorParameters>()).Result).Returns(list);

        //act
        var result = await this.client.FetchProducts(id);

        //verify
        CollectionAssert.AreEqual(result.Select(product => product.ArmTemplate).ToList(), list, this.comparer);
    }

    [TestMethod]
    public async Task FetchPolicy()
    {
        //arrange
        var id = "some-id";
        var content = "test content";
        var policy = new PolicyTemplateResource() { Properties = new PolicyTemplateProperties() { PolicyContent = content } };

        this.policyClient.Setup(client => client.GetPolicyLinkedToApiAsync(id, It.IsAny<ExtractorParameters>()).Result).Returns(policy);

        //act
        var result = await this.client.FetchPolicy(id);

        //verify
        Assert.AreEqual(content, result);
    }

    [TestMethod]
    public async Task FetchOperations()
    {
        //arrange
        var id = "api-id";
        List<ApiOperationTemplateResource> list = new();
        list.Add(new ApiOperationTemplateResource() { Name = "operation1-name", Properties = new ApiOperationProperties() { DisplayName = "display-name-1", Method = "POST", Description="some description"} });
        list.Add(new ApiOperationTemplateResource() { Name = "operation2-name", Properties = new ApiOperationProperties() { DisplayName = "display-name-2", Method = "GET", Description = "other description" } });

        this.apiOperationClient.Setup(client => client.GetOperationsLinkedToApiAsync(id, It.IsAny<ExtractorParameters>()).Result).Returns(list);

        //act
        var result = await this.client.FetchOperations(id);

        //verify
        CollectionAssert.AreEqual(result.Select(operation => operation.ArmTemplate).ToList(), list, this.comparer);
    }

    [TestMethod]
    public async Task FetchOperationPolicy()
    {
        //arrange
        var apiId = "api-id";
        var operationId = "operation-id";
        var content = "test policy";
        var policy = new PolicyTemplateResource() { Properties = new PolicyTemplateProperties() { PolicyContent = content } };

        this.policyClient.Setup(client => client.GetPolicyLinkedToApiOperationAsync(apiId, operationId, It.IsAny<ExtractorParameters>()).Result).Returns(policy);

        //act
        var result = await this.client.FetchOperationPolicy(apiId, operationId);

        //verify
        Assert.AreEqual(content, result);
    }

    [TestMethod]
    public async Task ExportOpenApiDefinition()
    {
        //arrange
        var apiId = "some-api-id";

        //act
        await this.client.ExportOpenApiDefinition(apiId);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Get);
        request.RequestUri.PathAndQuery.Should().EndWith($"/apis/{apiId}?export=true&format=openapi%2Bjson&api-version=2022-09-01-preview");
    }

    [TestMethod]
    public async Task ImportOpenAPIDefinition()
    {
        //arrange
        var api = new ApiTemplateResource() { Name = "test-api-1;rev=1", Properties = new ApiProperties { DisplayName = "test-api-dn-1" } };
        var apiDefinition = "definition payload";
        var apiId = "some-api-id";
        var workspaceId = "some-workspace";
        var locationHeader = "https://management.azure.com/someLocation";

        this.httpHandler.SetupRequest(request => request.RequestUri.Query.Contains("import"))
        .ReturnsResponse(HttpStatusCode.Accepted, response => response.Headers.Add("Location", locationHeader))
        .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });

        this.httpHandler.SetupRequest(locationHeader)
            .ReturnsResponse(HttpStatusCode.Created, new StringContent(JsonConvert.SerializeObject(api), Encoding.UTF8, "application/json"));

        //act

        var result = await this.client.ImportOpenApiDefinition(apiDefinition, apiId, workspaceId);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().EndWith($"/workspaces/{workspaceId}/apis/{apiId}?import=true&api-version=2022-09-01-preview");
        var body = await request.Content.ReadAsStringAsync();
        Assert.AreEqual(body, apiDefinition);

        Assert.IsTrue(result.ArmTemplate.TestEquality(api));
    }

    [TestMethod]
    public async Task Create()
    {
        //arrange
        var api = new ApiTemplateResource() { Name = "test-api-1;rev=1", OriginalName = "test-original-name-1", Properties = new ApiProperties { DisplayName = "test-api-dn-1" } };
        var workspaceId = "some-workspace";
        
        this.httpHandler.SetupAnyRequest()
        .ReturnsResponse(HttpStatusCode.Created, new StringContent(JsonConvert.SerializeObject(api), Encoding.UTF8, "application/json"))
        .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });

        //act
        var result = await this.client.Create(api, workspaceId);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().EndWith($"/workspaces/{workspaceId}/apis/{api.Name}?api-version=2022-09-01-preview");
        var body = await request.Content.ReadAsStringAsync();
        Assert.IsTrue(JsonConvert.DeserializeObject<ApiTemplateResource>(body).TestEquality(api));

        Assert.IsTrue(result.ArmTemplate.TestEquality(api));
    }

    [TestMethod]
    public async Task FetchTags()
    {
        //arrange
        var id = "test-id";
        List<TagTemplateResource> list = new();
        list.Add(new TagTemplateResource() { Properties = new TagProperties() { DisplayName = "test-tag" } });
        list.Add(new TagTemplateResource() { Properties = new TagProperties() { DisplayName = "test-tag2" } });
        this.tagClient.Setup(client => client.GetAllTagsLinkedToApiAsync(id, It.IsAny<ExtractorParameters>()).Result).Returns(list);

        //act
        var result = await this.client.FetchTags(id);

        //verify
        CollectionAssert.AreEqual(result.Select(tag => tag.ArmTemplate).ToList(), list, this.comparer);
    }

    [TestMethod]
    public async Task FetchOperationTags()
    {
        //arrange
        var apidId = "api-id";
        var operationId = "operation-id";
        List<TagTemplateResource> list = new();
        list.Add(new TagTemplateResource() { Properties = new TagProperties() { DisplayName = "test-tag" } });
        list.Add(new TagTemplateResource() { Properties = new TagProperties() { DisplayName = "test-tag2" } });
        this.tagClient.Setup(client => client.GetTagsLinkedToApiOperationAsync(apidId, operationId, It.IsAny<ExtractorParameters>()).Result).Returns(list);

        //act
        var result = await this.client.FetchOperationTags(apidId, operationId);

        //verify
        CollectionAssert.AreEqual(result.Select(tag => tag.ArmTemplate).ToList(), list, this.comparer);
    }
}