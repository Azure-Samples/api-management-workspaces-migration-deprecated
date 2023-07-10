using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Executor.Operations;
using Moq.Contrib.HttpClient;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MigrationTool.Migration.Domain.Operations;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Tests.Handlers;

[TestClass]
public class ApiOperationTagConnectionHandlerTests: BaseTest
{
    private ApiOperationTagConnectionHandler handler;

    [TestInitialize]
    public void Initialize()
    {
        this.handler = new ApiOperationTagConnectionHandler(tagClient.Object, entitiesRegistry);
    }

    [TestMethod]
    public async Task Handle()
    {
        //arrange
        var workspaceId = "workspace-id";
        var apiOperation = new OperationEntity("operation-id", "api-id");
        var newOperation = new OperationEntity("new-id", "new-api");
        var tag = new Entity("tag-id", EntityType.Tag);
        var newTag = new Entity("new-tag", EntityType.Tag);

        entitiesRegistry.RegisterMapping(apiOperation, newOperation);
        entitiesRegistry.RegisterMapping(tag, newTag);

        //act
        await this.handler.Handle(new ConnectOperation(apiOperation, tag), workspaceId);

        //verify
        tagClient.Verify(c => c.ConnectWithApiOperation(newTag, newOperation, workspaceId));
    }
}
