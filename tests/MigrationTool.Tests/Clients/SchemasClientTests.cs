using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.PolicyFragments;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using Moq.Contrib.HttpClient;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Schemas;
using FluentAssertions;
using Newtonsoft.Json;
using System.Text;
using MigrationTool.Tests.Helpers;

namespace MigrationTool.Tests.Clients;

[TestClass]
public class SchemasClientTests : BaseTest
{

    private ISchemasClient client;
    HttpRequestMessage request = null;

    [TestInitialize]
    public void Initialize()
    {
        this.client = new SchemasClient(extractorParameters, armSchemasClient.Object, httpHandler.CreateClientFactory(), auth.Object);

        httpHandler.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK)
            .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });
    }

    [TestMethod]
    public async Task Fetch()
    {
        //arrange
        var ids = new string[] { "schema-1", "schema-2" };
        List<SchemaTemplateResource> list = new()
        {
            new SchemaTemplateResource() { Name = ids[0], Properties = new SchemaProperties() { Description = "description1", SchemaType = "json", Value = "value1" } },
            new SchemaTemplateResource() { Name = ids[1], Properties = new SchemaProperties() { Description = "description2", SchemaType = "json", Value = "value2" } }
        };

        armSchemasClient.Setup(client => client.GetAllAsync(It.IsAny<ExtractorParameters>()).Result).Returns(list);

        //act

        var result = await this.client.Fetch(ids);

        //verify
        CollectionAssert.AreEqual(result.Select(item => item.ArmTemplate).ToList(), list, this.comparer);
    }

    [TestMethod]
    public async Task Create()
    {
        //arrange
        var schema = new SchemaTemplateResource() { Name = "id-1", Properties = new SchemaProperties() { Description = "description1", SchemaType = "json", Value = "value1" } };
        var workspaceId = "some-workspace";

        httpHandler.SetupAnyRequest()
        .ReturnsResponse(HttpStatusCode.Created, new StringContent(JsonConvert.SerializeObject(schema), Encoding.UTF8, "application/json"))
        .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });

        //act
        var result = await this.client.Create(schema, workspaceId);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().EndWith($"/workspaces/{workspaceId}/schemas/{schema.Name}?api-version=2022-09-01-preview");
        var body = await request.Content.ReadAsStringAsync();
        Assert.IsTrue(JsonConvert.DeserializeObject<SchemaTemplateResource>(body).TestEquality(schema));

        Assert.IsTrue(result.ArmTemplate.TestEquality(schema));
    }
}
