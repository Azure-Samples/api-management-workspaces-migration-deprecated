using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Clients;
using Moq.Contrib.HttpClient;
using System.Net.Http;
using System.Net;
using System.Threading;
using FluentAssertions;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Groups;
using MigrationTool.Tests.Helpers;
using System.Collections.Generic;
using MigrationTool.Migration.Domain.Entities;
using System.Linq;

namespace MigrationTool.Tests.Clients;

[TestClass]
public class GroupsClientTests : BaseTest
{
    private GroupsClient client;
    HttpRequestMessage request = null;

    [TestInitialize]
    public void Initialize()
    {
        this.client = new GroupsClient(productClient.Object, httpHandler.CreateClientFactory(), extractorParameters, auth.Object);

        httpHandler.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK)
            .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });
    }

    [TestMethod]
    public async Task Create()
    {
        //arrange
        var group = new GroupTemplateResource() { Name = "test-group-1", OriginalName = "test-original-name-1", Properties = new GroupProperties { DisplayName = "test-group-dn-1" } };
        var workspaceId = "some-workspace";

        httpHandler.SetupAnyRequest()
        .ReturnsResponse(HttpStatusCode.Created, new StringContent(JsonConvert.SerializeObject(group), Encoding.UTF8, "application/json"))
        .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });

        //act
        var result = await this.client.Create(group, workspaceId);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().EndWith($"/workspaces/{workspaceId}/groups/{group.Name}?api-version=2022-09-01-preview");
        var body = await request.Content.ReadAsStringAsync();
        Assert.IsTrue(JsonConvert.DeserializeObject<GroupTemplateResource>(body).TestEquality(group));

        Assert.IsTrue(result.ArmTemplate.TestEquality(group));
    }

    [TestMethod]
    public async Task FetchEntities()
    {
        //arrange
        var groupId = "some-group";
        List<Entity> products = new()
        {
            new Entity("test-product-1", EntityType.Product, "test-product-dn-1", null),
            new Entity("test-product-2", EntityType.Product, "test-product-dn-2", null),
            new Entity("test-product-3", EntityType.Product, "test-product-dn-3", null)
        };
        List<Entity> groups = new()
        {
            new Entity(groupId, EntityType.Group),
            new Entity("another-group", EntityType.Group)
        };
        productClient.Setup(client => client.FetchAll().Result).Returns(products);
        productClient.Setup(client => client.FetchGroups("test-product-1").Result).Returns(groups.GetRange(0,1));
        productClient.Setup(client => client.FetchGroups("test-product-2").Result).Returns(groups);
        productClient.Setup(client => client.FetchGroups("test-product-3").Result).Returns(groups.GetRange(1,1));


        //act
        var result = await this.client.FetchProducts(groupId);

        //verify
        CollectionAssert.AreEqual(result.Select(product => product).ToList(), products.GetRange(0,2));
    }

    [TestMethod]
    public async Task ConnectWithProduct()
    {
        //arrange
        var workspaceId = "workspace-id";
        var group = new Entity("group-id", EntityType.Group);
        var product = new Entity("product-id", EntityType.Product);

        //act
        await this.client.ConnectWithProduct(group, product, workspaceId);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().Contain($"/workspaces/{workspaceId}/products/{product.Id}/groupLinks");
        var body = await request.Content.ReadAsStringAsync();
        body.Should().Contain(group.Id);
    }

    [TestMethod]
    public async Task ConnectWithUser()
    {

        //arrange
        var workspaceId = "workspace-id";
        var group = new Entity("group-id", EntityType.Group);
        var user = new Entity("user-id", EntityType.User);

        //act
        await this.client.ConnectWithUser(group, user, workspaceId);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().Contain($"/workspaces/{workspaceId}/groups/{group.Id}/users/{user.Id}");
    }
}
