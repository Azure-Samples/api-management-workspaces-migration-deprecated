using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class ProductGroupConnectionHandler : OperationHandler
{
    private readonly IGroupsClient _groupsClient;
    public ProductGroupConnectionHandler(IGroupsClient groupsClient, EntitiesRegistry registry) : base(registry)
    {
        this._groupsClient = groupsClient;
    }
    public override EntityType UsedEntities => EntityType.Product | EntityType.Group;

    public override Type OperationType => typeof(ConnectOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var connectOperation = this.GetOperationOrThrow<ConnectOperation>(operation);

        Entity product;
        Entity group;

        if (!this.tryGetNewEntity(connectOperation, EntityType.Group, out group))
        {
            throw new Exception($"Group {group.Id} not found");
        }
        if (!this.tryGetNewEntity(connectOperation, EntityType.Product, out product))
        {
            throw new Exception($"Product {product.Id} not found");
        }
        return this._groupsClient.ConnectWithProduct(group, product, workspaceId);
    }
}
