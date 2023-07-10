
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class ProductTagConnectionHandler : OperationHandler
{
    private ITagClient tagClient;
    public ProductTagConnectionHandler( ITagClient tagClient, EntitiesRegistry registry ) : base(registry)
    {
        this.tagClient = tagClient;
    }
    public override EntityType UsedEntities => EntityType.Product | EntityType.Tag;

    public override Type OperationType => typeof(ConnectOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var connectOperation = this.GetOperationOrThrow<ConnectOperation>(operation);

        Entity product;
        Entity tag;

        if (!this.tryGetNewEntity(connectOperation, EntityType.Tag, out tag))
        {
            throw new Exception($"Tag {tag.Id} not found");
        }
        if (!this.tryGetNewEntity(connectOperation, EntityType.Product, out product))
        {
            throw new Exception($"Product {product.Id} not found");
        }
        return this.tagClient.ConnectWithProduct(tag, product, workspaceId);
    }
}
