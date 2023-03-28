using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class ProductApiConnectionHandler : OperationHandler
{
    private readonly ProductClient productClient;
    private readonly EntitiesRegistry registry;

    public ProductApiConnectionHandler(ProductClient productClient, EntitiesRegistry registry)
    {
        this.productClient = productClient;
        this.registry = registry;
    }

    public override EntityType UsedEntities => EntityType.Product | EntityType.Api;
    public override Type OperationType => typeof(ConnectOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var connectOperation = this.GetOperationOrThrow<ConnectOperation>(operation);

        var product = this.GetProduct(connectOperation);
        var api = this.GetApi(connectOperation);

        return this.productClient.AddApi(product, api, workspaceId);
    }

    private Entity GetApi(ConnectOperation connectOperation) =>
        this.GetNewEntity(connectOperation, EntityType.Api);

    private Entity GetProduct(ConnectOperation connectOperation) =>
        this.GetNewEntity(connectOperation, EntityType.Product);

    private Entity GetNewEntity(ConnectOperation connectOperation, EntityType entityType)
    {
        var originalEntity = connectOperation.Entity.Type == entityType
            ? connectOperation.Entity
            : connectOperation.ConnectToEntity;
        if (!this.registry.TryGetMapping(originalEntity, out var newEntity))
            throw new Exception();
        return newEntity;
    }
}