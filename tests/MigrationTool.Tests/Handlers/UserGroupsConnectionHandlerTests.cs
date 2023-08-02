using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Migration.Domain.Operations;
using System.Threading.Tasks;

namespace MigrationTool.Tests.Handlers;

[TestClass]
public class UserGroupsConnectionHandlerTests : BaseTest
{
    private UserGroupConnectionHandler handler;

    [TestInitialize]
    public void Initialize()
    {
        this.handler = new UserGroupConnectionHandler(groupsClient.Object, entitiesRegistry);
    }

    [TestMethod]
    public async Task Handle()
    {
        //arrange
        var workspaceId = "workspace-id";
        var group = new Entity("group-id", EntityType.Group);
        var newGroup = new Entity("new-group-id", EntityType.Group);
        var user = new Entity("user-1", EntityType.User);

        entitiesRegistry.RegisterMapping(group, newGroup);

        //act
        await this.handler.Handle(new ConnectOperation(group, user), workspaceId);

        //verify
        groupsClient.Verify(c => c.ConnectWithUser(newGroup, user, workspaceId));
    }
}
