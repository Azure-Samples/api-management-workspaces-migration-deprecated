using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Migration.Domain.Operations;
using System.Threading.Tasks;
namespace MigrationTool.Tests.Handlers;

[TestClass]
public class ProductTagConnectionHandlerTests: BaseTest
{
    private ProductTagConnectionHandler handler;

    [TestInitialize]
    public void Initialize()
    {
        this.handler = new ProductTagConnectionHandler(tagClient.Object, entitiesRegistry);
    }

    [TestMethod]
    public async Task Handle()
    {
        //arrange
        var workspaceId = "workspace-id";
        var product = new Entity("product-id", EntityType.Product);
        var newProduct = new Entity("new-product", EntityType.Product);
        var tag = new Entity("tag-id", EntityType.Tag);
        var newTag = new Entity("new-tag", EntityType.Tag);

        entitiesRegistry.RegisterMapping(product, newProduct);
        entitiesRegistry.RegisterMapping(tag, newTag);

        //act
        await this.handler.Handle(new ConnectOperation(product, tag), workspaceId);

        //verify
        tagClient.Verify(c => c.ConnectWithProduct(newTag, newProduct, workspaceId));
    }
}
