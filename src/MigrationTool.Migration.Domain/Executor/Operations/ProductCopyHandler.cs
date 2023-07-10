using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ProductApis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Products;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class ProductCopyOperationHandler : OperationHandler
{
    private readonly IProductClient productClient;
    private readonly PolicyModifier policyModifier;

    public ProductCopyOperationHandler(IProductClient productClient,
        EntitiesRegistry registry,
        PolicyModifier policyModifier) : base (registry)
    {
        this.productClient = productClient;
        this.policyModifier = policyModifier;
    }

    public override EntityType UsedEntities => EntityType.Product;
    public override Type OperationType => typeof(CopyOperation);

    public override async Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);

        var originalProduct = copyOperation.Entity;
        var modifiedTemplate = ModifyTemplate(workspaceId, (ProductApiTemplateResource)originalProduct.ArmTemplate);

        var newProduct = await this.productClient.CreateProduct(modifiedTemplate, workspaceId);
        this.registry.RegisterMapping(originalProduct, newProduct);

        var policy = await this.productClient.FetchPolicy(originalProduct.Id);
        if (policy != null)
        {
            var modifiedPolicy = this.policyModifier.Modify(policy);
            await this.productClient.UploadProductPolicy(newProduct, modifiedPolicy, workspaceId);
        }
    }

    static ProductApiTemplateResource ModifyTemplate(string workspaceId, ProductApiTemplateResource template)
    {
        var productTemplate = template.Copy();
        productTemplate.Name = $"{productTemplate.Name}-in-{workspaceId}";
        productTemplate.Properties.DisplayName = $"{productTemplate.Properties.DisplayName}-in-{workspaceId}";
        return productTemplate;
    }
}