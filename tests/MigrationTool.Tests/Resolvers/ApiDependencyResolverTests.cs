using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Dependencies.Resolvers;
using MigrationTool.Migration.Domain.Entities;
using System.Threading.Tasks;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace MigrationTool.Tests.Resolvers;

[TestClass]
public class ApiDependencyResolverTests : BaseTest
{
    private ApiDependencyResolver resolver;

    [TestInitialize]
    public void Initialize()
    {
        subscriptionClient.Setup(client => client.FetchForApi(It.IsAny<string>()).Result).Returns(new List<Entity>());
        apiClient.Setup(client => client.FetchProducts(It.IsAny<string>()).Result).Returns(new List<Entity>());
        apiClient.Setup(client => client.FetchTags(It.IsAny<string>()).Result).Returns(new List<Entity>());
        gatewayClient.Setup(client => client.IsLinkedWithGateway(It.IsAny<ApiEntity>()).Result).Returns(false);
        policyRelatedDependencyResolver.Setup(resolver => resolver.Resolve(It.IsAny<string>()).Result).Returns(new List<Entity>());
        versionSetClient.Setup(client => client.FetchVersionSet(It.IsAny<Entity>()).Result).Returns((Entity) null);
        this.resolver = new ApiDependencyResolver(apiClient.Object, subscriptionClient.Object, policyRelatedDependencyResolver.Object, versionSetClient.Object, tagsDependencyResolver.Object, gatewayClient.Object);
    }

    [TestMethod]
    public async Task ResolvesApiOperations()
    {
        //arrange
        var entity = new ApiEntity("some-id");
        List<OperationEntity> list = new();
        list.Add(new OperationEntity("some-id-1", entity.Id));
        list.Add(new OperationEntity("some-id-2", entity.Id));

        apiClient.Setup(client => client.FetchOperations(It.IsAny<string>()).Result).Returns(list);

        //act
        var result = await this.resolver.ResolveDependencies(entity);

        //verify
        CollectionAssert.AreEqual(result.ToList(), list);
    }
}
