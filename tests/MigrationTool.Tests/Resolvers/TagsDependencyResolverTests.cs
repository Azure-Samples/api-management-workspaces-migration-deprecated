using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Dependencies.Resolvers;
using MigrationTool.Migration.Domain.Entities;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MigrationTool.Tests.Resolvers;

[TestClass]
public class TagsDependencyResolverTests: BaseTest
{
    private TagsDependencyResolver resolver;

    [TestInitialize]
    public void Initialize()
    {
        resolver = new TagsDependencyResolver(tagClient.Object);
    }

    [TestMethod]
    public async Task Resolve()
    {
        //arrange
        var api = new ApiEntity("api-id");
        var product = new Entity("product-id", EntityType.Product);
        var apiOperation = new OperationEntity("operation-id", "another-api-id");

        var tag = new Entity("tag", EntityType.Tag);
        tagClient.Setup(client => client.FetchEntities(It.IsAny<string>()).Result).Returns(new List<Entity>() { api, product, apiOperation });

        //act
        var result = await resolver.ResolveDependencies(tag);

        //verify
        tagClient.Verify(client => client.FetchEntities(tag.Id));
        CollectionAssert.Contains(result.ToList(), api);
        CollectionAssert.Contains(result.ToList(), product);
        CollectionAssert.Contains(result.ToList(), apiOperation);
    }
}
