using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor;

public interface IOperationHandler
{
    public EntityType UsedEntities { get; }

    public Type OperationType { get; }

    public Task Handle(IMigrationOperation operation, string workspaceId);
}