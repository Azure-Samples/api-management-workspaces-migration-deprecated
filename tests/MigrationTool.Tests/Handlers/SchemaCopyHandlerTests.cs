using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.PolicyFragments;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Schemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Migration.Domain.Operations;
using Moq;
using System.Threading.Tasks;

namespace MigrationTool.Tests.Handlers;

[TestClass]
public class SchemaCopyHandlerTests : BaseTest
{
    private SchemaCopyHandler handler;

    [TestInitialize]
    public void Initialize()
    {
        this.handler = new SchemaCopyHandler(schemasClient.Object, entitiesRegistry);
    }

    [TestMethod]
    public async Task Handle()
    {
        //arrange
        var workspaceId = "workspace-id";
        var schema = new Entity("some-id", EntityType.Schema, "name", new SchemaTemplateResource() { Name = "some-id", Properties = new SchemaProperties() { Description = "description1", SchemaType = "xml", Value = "value1" } });

        //act
        await this.handler.Handle(new CopyOperation(schema), workspaceId);
        var gotMapping = this.entitiesRegistry.TryGetMapping(schema, out var newSchema);

        //verify
        schemasClient.Verify(c => c.Create(It.IsAny<SchemaTemplateResource>(), workspaceId));
        Assert.IsTrue(gotMapping);
        Assert.AreEqual(newSchema.ArmTemplate.Name, schema.ArmTemplate.Name + "-in-" + workspaceId);
        Assert.AreEqual(((SchemaTemplateResource)newSchema.ArmTemplate).Name, ((SchemaTemplateResource)schema.ArmTemplate).Name + "-in-" + workspaceId);
    }
}
