using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Dependencies.Resolvers;
using MigrationTool.Migration.Domain.Entities;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MigrationTool.Tests.Resolvers;

[TestClass]
public class GroupsDependencyResolverTests : BaseTest
{
    private GroupDependencyResolver resolver;

    [TestInitialize]
    public void Initialize()
    {
        groupsClient.Setup(client => client.FetchProducts(It.IsAny<string>()).Result).Returns(new List<Entity>());
        groupsClient.Setup(client => client.FetchUsers(It.IsAny<string>()).Result).Returns(new List<Entity>());
        this.resolver = new GroupDependencyResolver(groupsClient.Object);
    }

    [TestMethod]
    public async Task ResolveUsers()
    {
        //arrange
        var entity = new Entity("some-id", EntityType.Group);
        List<Entity> list = new();
        list.Add(new Entity("some-id-1", EntityType.User));
        list.Add(new Entity("some-id-2", EntityType.User));

        groupsClient.Setup(client => client.FetchUsers(It.IsAny<string>()).Result).Returns(list);

        //act
        var result = await this.resolver.ResolveDependencies(entity);

        //verify
        CollectionAssert.AreEqual(result.ToList(), list);
    }

    [TestMethod]
    public async Task ResolveProducts()
    {
        //arrange
        var entity = new Entity("some-id", EntityType.Group);
        List<Entity> list = new();
        list.Add(new Entity("some-id-1", EntityType.Product));
        list.Add(new Entity("some-id-2", EntityType.Product));

        groupsClient.Setup(client => client.FetchProducts(It.IsAny<string>()).Result).Returns(list);

        //act
        var result = await this.resolver.ResolveDependencies(entity);

        //verify
        CollectionAssert.AreEqual(result.ToList(), list);
    }
}
