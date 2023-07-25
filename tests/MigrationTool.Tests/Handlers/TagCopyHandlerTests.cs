using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Migration.Domain.Operations;
using System.Threading.Tasks;
using Moq;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Tags;

namespace MigrationTool.Tests.Handlers;

[TestClass]
public class TagCopyHandlerTests: BaseTest
{
    private TagCopyHandler handler;

    [TestInitialize]
    public void Initialize()
    {
        this.handler = new TagCopyHandler(tagClient.Object, entitiesRegistry);
    }

    [TestMethod]
    public async Task Handle()
    {
        //arrange
        var workspaceId = "workspace-id";
        var tag = new Entity("tag-id", EntityType.Tag, "display-name" , new TagTemplateResource() { Name = "tag-name", Properties = new TagProperties() { DisplayName = "display-name" } });

        //act
        await this.handler.Handle(new CopyOperation(tag), workspaceId);
        var gotMapping = this.entitiesRegistry.TryGetMapping(tag, out var newTag);

        //verify
        tagClient.Verify(c => c.Create(It.IsAny<TagTemplateResource>(), workspaceId));
        Assert.IsTrue(gotMapping);
        Assert.AreEqual(newTag.ArmTemplate.Name, tag.ArmTemplate.Name + "-in-" + workspaceId);
        Assert.AreEqual(((TagTemplateResource) newTag.ArmTemplate).Properties.DisplayName, ((TagTemplateResource) tag.ArmTemplate).Properties.DisplayName + "-in-" + workspaceId);
    }
}
