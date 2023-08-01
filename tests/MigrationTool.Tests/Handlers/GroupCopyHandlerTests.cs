using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Groups;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Tags;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Migration.Domain.Operations;
using Moq;
using System.Threading.Tasks;

namespace MigrationTool.Tests.Handlers;

[TestClass]
public class GroupCopyHandlerTests : BaseTest
{
    private GroupCopyHandler handler;

    [TestInitialize]
    public void Initialize()
    {
        this.handler = new GroupCopyHandler(groupsClient.Object, entitiesRegistry);
    }

    [TestMethod]
    public async Task Handle()
    {
        //arrange
        var workspaceId = "workspace-id";
        var group = new Entity("group-id", EntityType.Group, "display-name", new GroupTemplateResource() { Name = "group-name", Properties = new GroupProperties() { DisplayName = "display-name" } });

        //act
        await this.handler.Handle(new CopyOperation(group), workspaceId);
        var gotMapping = this.entitiesRegistry.TryGetMapping(group, out var newGroup);

        //verify
        groupsClient.Verify(c => c.Create(It.IsAny<GroupTemplateResource>(), workspaceId));
        Assert.IsTrue(gotMapping);
        Assert.AreEqual(newGroup.ArmTemplate.Name, group.ArmTemplate.Name + "-in-" + workspaceId);
        Assert.AreEqual(((GroupTemplateResource)newGroup.ArmTemplate).Properties.DisplayName, ((GroupTemplateResource)group.ArmTemplate).Properties.DisplayName + "-in-" + workspaceId);
    }
}
