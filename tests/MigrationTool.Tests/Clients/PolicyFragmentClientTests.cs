using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Clients;
using Moq.Contrib.HttpClient;
using System.Net.Http;
using System.Net;
using System.Threading;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Products;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.PolicyFragments;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using System.Text;
using MigrationTool.Tests.Helpers;

namespace MigrationTool.Tests.Clients;

[TestClass]
public class PolicyFragmentClientTests : BaseTest
{
    private IPolicyFragmentClient client;
    HttpRequestMessage request = null;

    [TestInitialize]
    public void Initialize()
    {
        this.client = new PolicyFragmentClient(extractorParameters, armPolicyFragmentsClient.Object, httpHandler.CreateClientFactory(), auth.Object);

        httpHandler.SetupAnyRequest()
            .ReturnsResponse(HttpStatusCode.OK)
            .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });
    }

    [TestMethod]
    public async Task Fetch()
    {
        //arrange
        var ids = new string[] { "pf-1", "pf-2" };
        List<PolicyFragmentsResource> list = new()
        {
            new PolicyFragmentsResource() { Name = ids[0], Properties = new PolicyFragmentsProperties() { Description = "description1", Format = "format1", Value = "value1" } },
            new PolicyFragmentsResource() { Name = ids[1], Properties = new PolicyFragmentsProperties() { Description = "description2", Format = "format2", Value = "value2" } }
        };

        armPolicyFragmentsClient.Setup(client => client.GetAllAsync(It.IsAny<ExtractorParameters>()).Result).Returns(list);

        //act

        var result = await this.client.Fetch(ids);

        //verify
        CollectionAssert.AreEqual(result.Select(item => item.ArmTemplate).ToList(), list, this.comparer); //check if both apis where imported correctly
    }

    [TestMethod]
    public async Task Create()
    {
        //arrange
        var policyFragment = new PolicyFragmentsResource() { Name = "id-1", Properties = new PolicyFragmentsProperties() { Description = "description1", Format = "format1", Value = "value1" } };
        var workspaceId = "some-workspace";

        httpHandler.SetupAnyRequest()
        .ReturnsResponse(HttpStatusCode.Created, new StringContent(JsonConvert.SerializeObject(policyFragment), Encoding.UTF8, "application/json"))
        .Callback<HttpRequestMessage, CancellationToken>((e, c) => { request = e; });

        //act
        var result = await this.client.Create(policyFragment, workspaceId);

        //verify
        Assert.AreEqual(request.Method, HttpMethod.Put);
        request.RequestUri.PathAndQuery.Should().EndWith($"/workspaces/{workspaceId}/policyFragments/{policyFragment.Name}?api-version=2022-09-01-preview");
        var body = await request.Content.ReadAsStringAsync();
        Assert.IsTrue(JsonConvert.DeserializeObject<PolicyFragmentsResource>(body).TestEquality(policyFragment));

        Assert.IsTrue(result.ArmTemplate.TestEquality(policyFragment));
    }
}
