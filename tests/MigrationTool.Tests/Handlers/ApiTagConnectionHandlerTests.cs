using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Migration.Domain.Operations;
using System.Threading.Tasks;

namespace MigrationTool.Tests.Handlers;

[TestClass]
public class ApiTagConnectionHandlerTests: BaseTest
{
    private ApiTagConnectionHandler handler;

    [TestInitialize]
    public void Initialize()
    {
        this.handler = new ApiTagConnectionHandler(tagClient.Object, entitiesRegistry);
    }

    [TestMethod]
    public async Task Handle()
    {
        //arrange
        var workspaceId = "workspace-id";
        var api = new ApiEntity( "api-id");
        var newApi = new ApiEntity("new-api");
        var tag = new Entity("tag-id", EntityType.Tag);
        var newTag = new Entity("new-tag", EntityType.Tag);

        entitiesRegistry.RegisterMapping(api, newApi);
        entitiesRegistry.RegisterMapping(tag, newTag);

        //act
        await this.handler.Handle(new ConnectOperation(api, tag), workspaceId);

        //verify
        tagClient.Verify(c => c.ConnectWithApi(newTag, newApi, workspaceId));
    }
}
