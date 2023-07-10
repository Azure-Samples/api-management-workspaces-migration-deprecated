using FluentAssertions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Tags;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using Moq.Contrib.HttpClient;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace MigrationTool.Tests.Clients;

[TestClass]
public class TagClientTests : BaseTest
{
    private TagClient client;
    HttpRequestMessage request = null;

    [TestInitialize]
    public void Initialize()
    {
        this.client = new TagClient(httpHandler.CreateClientFactory(), extractorParameters, productClient.Object, apiClient.Object, auth.Object);

        httpHandler.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK)
            .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });
    }

    [TestMethod]
    public async Task FetchEntities()
    {
        //arrange
        var tagId = "some-tag";
        var api = new ApiEntity("api-id");
        var operation = new Entity("operation-id", EntityType.ApiOperation);
        var product = new Entity("product-id", EntityType.Product);
        var response = new { value = new object[] { 
            new { operation = new { id = $"/apis/{api.Id}/operations/{operation.Id}", apiName=api.Id } },
            new { product = new { id = $"/products/{product.Id}" }},
            new { api = new {id = $"/apis/{api.Id}"}}
        } };

        httpHandler.SetupAnyRequest()
            .ReturnsJsonResponse(HttpStatusCode.OK, response)
            .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });

        productClient.Setup(client => client.Fetch(It.IsAny<string>()).Result).Returns(product);
        apiClient.Setup(aclient => aclient.Fetch(It.IsAny<string>()).Result).Returns(api);
        List<Entity> operations = new();
        operations.Add(operation);
        operations.Add(new Entity("another-operation", EntityType.ApiOperation));
        apiClient.Setup(aclient => aclient.FetchOperations(It.IsAny<string>()).Result).Returns(operations);


        //act
        var result = await this.client.FetchEntities(tagId);

        //verify
        Assert.AreEqual(result.Where(obj => obj.Type == EntityType.Api).First(), api);
        Assert.AreEqual(result.Where(obj => obj.Type == EntityType.Product).First(), product);
        Assert.AreEqual(result.Where(obj => obj.Type == EntityType.ApiOperation).First(), operation);
    }

    [TestMethod]
    public async Task Create()
    {
        //arrange
        var workspace = "workspace-id";
        var template = new TagTemplateResource() { OriginalName = "tag-name", Properties = new TagProperties() { DisplayName = "tag-display-name" } };

        //act
        await this.client.Create(template, workspace);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().EndWith($"/workspaces/{workspace}/tags/{template.Properties.DisplayName}?api-version=2022-09-01-preview");
        var body = await request.Content.ReadAsStringAsync();
        //Assert.AreEqual(body, template.ToString());
        body.Should().BeEquivalentTo("{\"properties\":{\"displayName\":\"tag-display-name\"},\"originalName\":\"tag-name\"}");
    }

    [TestMethod]
    public async Task ConnectWithApi()
    {
        //arrange
        var tag = new Entity("tag-id", EntityType.Tag) { DisplayName = "tag-display-name" };
        var api = new ApiEntity("api-id");
        var workspace = "workspace-id";

        //act
        await this.client.ConnectWithApi(tag, api, workspace);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().Contain($"/workspaces/{workspace}/tags/{tag.DisplayName}/apiLinks/");
        var body = await request.Content.ReadAsStringAsync();
        body.Should().Contain(api.Id).And.Contain("apiId");
    }

    [TestMethod]
    public async Task ConnectWithProduct()
    {
        //arrange
        var tag = new Entity("tag-id", EntityType.Tag) { DisplayName = "tag-display-name" };
        var product = new Entity("product-id", EntityType.Product);
        var workspace = "workspace-id";

        //act
        await this.client.ConnectWithProduct(tag, product, workspace);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().Contain($"/workspaces/{workspace}/tags/{tag.DisplayName}/productLinks/");
        var body = await request.Content.ReadAsStringAsync();
        body.Should().Contain(product.Id).And.Contain("productId");
    }

    [TestMethod]
    public async Task ConnectWithApiOperation()
    {
        //arrange
        var tag = new Entity("tag-id", EntityType.Tag) { DisplayName = "tag-display-name" };
        var operation = new OperationEntity("operation-id", "api-id");
        var workspace = "workspace-id";

        //act
        await this.client.ConnectWithApiOperation(tag, operation, workspace);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().Contain($"/workspaces/{workspace}/tags/{tag.DisplayName}/operationLinks/");
        var body = await request.Content.ReadAsStringAsync();
        body.Should().Contain(operation.Id).And.Contain("operationId").And.Contain(operation.ApiId);
    }
}
