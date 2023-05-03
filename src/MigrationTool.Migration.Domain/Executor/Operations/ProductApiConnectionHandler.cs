using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;
using System.Diagnostics.CodeAnalysis;

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

        Entity product;
        Entity api;

        this.tryGetNewEntity(connectOperation, EntityType.Api, out api);
        if (!this.tryGetNewEntity(connectOperation, EntityType.Product, out product))
        {
            return this.productClient.AddApi(product, api, null, workspaceId);
        }
        return this.productClient.AddApi(product, api, workspaceId, workspaceId);
    }

    private bool tryGetNewEntity(ConnectOperation connectOperation, EntityType entityType, out Entity entity)
    {
        var originalEntity = connectOperation.Entity.Type == entityType
            ? connectOperation.Entity
            : connectOperation.ConnectToEntity;
        if (!this.registry.TryGetMapping(originalEntity, out var newEntity))
        {
            entity = originalEntity;
            return false;
        }
        entity = newEntity;
        return true;
    }

}