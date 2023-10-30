using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class ProductApiConnectionHandler : OperationHandler
{
    private readonly IProductClient productClient;

    public ProductApiConnectionHandler(IProductClient productClient, EntitiesRegistry registry) : base(registry)
    {
        this.productClient = productClient;
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

}