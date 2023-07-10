using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Products;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Tags;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Clients;
using Moq;
using Moq.Contrib.HttpClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MigrationTool.Tests.Clients;

[TestClass]
public class ProductClientTests : BaseTest
{
    private ProductClient client;

    [TestInitialize]
    public void Initialize()
    {
        this.client = new ProductClient(extractorParameters, armApisClient.Object, armPolicyClient.Object, httpHandler.CreateClientFactory(), armApiRevisionClient.Object, armApiDataProcessor, armProductsClient.Object, armTagClient.Object, auth.Object);
    }

    [TestMethod]
    public async Task FetchTags()
    {
        //arrange
        List<TagTemplateResource> list = new();
        list.Add(new TagTemplateResource() { Name = "test-tag-1", OriginalName = "test-original-name-1", Properties = new TagProperties { DisplayName = "test-tag-dn-1" } });
        list.Add(new TagTemplateResource() { Name = "test-tag-2", OriginalName = "test-original-name-2", Properties = new TagProperties { DisplayName = "test-tag-dn-2" } });

        armTagClient.Setup(client => client.GetAllTagsLinkedToProductAsync(It.IsAny<string>(), It.IsAny<ExtractorParameters>()).Result).Returns(list);

        //act
        var result = await client.FetchTags("some-id");

        //verify
        CollectionAssert.AreEqual(result.Select(item => item.ArmTemplate).ToList(), list, comparer);
    }

    [TestMethod]
    public async Task FetchTagsEmpty()
    {
        //arrange
        List<TagTemplateResource> list = new();

        armTagClient.Setup(client => client.GetAllTagsLinkedToProductAsync(It.IsAny<string>(), It.IsAny<ExtractorParameters>()).Result).Returns(list);

        //act
        var result = await client.FetchTags("some-id");

        //verify
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task Fetch()
    {
        //arrange
        var searchedId = "test-product-1";
        List<ProductsTemplateResource>  list = new();
        list.Add(new ProductsTemplateResource() { Name = searchedId, Properties = new ProductsProperties() { DisplayName = "test-product-dn-1", ApprovalRequired = true, SubscriptionRequired = true, Name = null } });
        list.Add(new ProductsTemplateResource() { Name = "test-product-2", Properties = new ProductsProperties() { DisplayName = "test-product-dn-2", ApprovalRequired = true, SubscriptionRequired = true, Name = null } });

        armProductsClient.Setup(client => client.GetAllAsync(It.IsAny<ExtractorParameters>()).Result).Returns(list);

        //act

        var result = await this.client.Fetch(searchedId);

        //verify
        Assert.AreEqual(0, this.comparer.Compare(result.ArmTemplate, list[0]));
    }
}
