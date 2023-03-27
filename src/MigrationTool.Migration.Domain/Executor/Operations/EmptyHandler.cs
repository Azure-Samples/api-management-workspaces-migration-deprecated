using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class EmptyHandler : OperationHandler
{
    public EmptyHandler(EntityType usedEntities, Type operationType)
    {
        this.UsedEntities = usedEntities;
        this.OperationType = operationType;
    }

    public override EntityType UsedEntities { get; }
    public override Type OperationType { get; }
    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        return Task.CompletedTask;
    }
}