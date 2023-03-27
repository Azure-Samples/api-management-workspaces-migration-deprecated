using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class ProductCopyOperationHandler : OperationHandler
{
    private readonly ProductClient productClient;
    private readonly EntitiesRegistry registry;

    public ProductCopyOperationHandler(ProductClient productClient, EntitiesRegistry registry)
    {
        this.productClient = productClient;
        this.registry = registry;
    }

    public override EntityType UsedEntities => EntityType.Product;
    public override Type OperationType => typeof(CopyOperation);

    public override async Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);

        var originalProduct = copyOperation.Entity;

        var newProduct = await this.productClient.CreateProduct(originalProduct, IdModifier, workspaceId);
        this.registry.RegisterMapping(originalProduct, newProduct);

        var policy = await this.productClient.FetchPolicy(originalProduct.Id);
        if (policy != null)
            await this.productClient.UploadProductPolicy(newProduct, policy, workspaceId);

        string IdModifier(string id) => $"{id}-in-{workspaceId}";
    }
}