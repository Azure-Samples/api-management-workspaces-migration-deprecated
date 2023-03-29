
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class VersionSetCopyOperationHandler : OperationHandler
{
    private readonly VersionSetClient VersionSetClient;
    private readonly EntitiesRegistry Registry;

    public VersionSetCopyOperationHandler(VersionSetClient versionSetClient, EntitiesRegistry registry)
    {
        this.VersionSetClient = versionSetClient;
        this.Registry = registry;
    }

    public override EntityType UsedEntities => EntityType.VersionSet;
    public override Type OperationType => typeof(CopyOperation);

    public override async Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);

        var originalVersionSet = copyOperation.Entity as VersionSetEntity ?? throw new InvalidOperationException();
        var versionSetTemplate = originalVersionSet.ArmTemplate.Copy();
        versionSetTemplate.Name = $"{versionSetTemplate.Name}-in-{workspaceId}";
        versionSetTemplate.Properties.DisplayName = $"{versionSetTemplate.Properties.DisplayName}-in-{workspaceId}";
        
        var newVersionSet = await this.VersionSetClient.Create(versionSetTemplate, workspaceId);
        this.Registry.RegisterMapping(originalVersionSet, newVersionSet);
    }
}
