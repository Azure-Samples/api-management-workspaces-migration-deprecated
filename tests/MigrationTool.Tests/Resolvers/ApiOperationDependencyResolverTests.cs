using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Dependencies.Resolvers;
using MigrationTool.Migration.Domain.Entities;
using System.Collections.Generic;
using Moq;
using System.Threading.Tasks;
using System.Linq;

namespace MigrationTool.Tests.Resolvers;

[TestClass]
public class ApiOperationDependencyResolverTests: BaseTest
{
    private ApiOperationDependencyResolver resolver;

    [TestInitialize]
    public void Initialize()
    {
        apiClient.Setup(client => client.FetchOperationPolicy(It.IsAny<string>(), It.IsAny<string>()).Result).Returns("some-policy");

        this.resolver = new ApiOperationDependencyResolver(apiClient.Object, policyRelatedDependencyResolver.Object);
    }

    [TestMethod]
    public async Task ResolveDependencies()
    {
        //arrange
        var operation = new OperationEntity("operation-id", "api-id");
        var namedValue = new Entity("named-value", EntityType.NamedValue);
        var tag = new Entity("tag", EntityType.Tag);
        policyRelatedDependencyResolver.Setup(resolver => resolver.Resolve(It.IsAny<string>()).Result).Returns(new List<Entity>() { namedValue });
        apiClient.Setup(client => client.FetchOperationTags(It.IsAny<string>(), It.IsAny<string>()).Result).Returns(new List<Entity> { tag });

        //act
        var result = await this.resolver.ResolveDependencies(operation);

        //verify
        CollectionAssert.Contains(result.ToList(), namedValue);
        CollectionAssert.Contains(result.ToList(), tag);
    }
}
