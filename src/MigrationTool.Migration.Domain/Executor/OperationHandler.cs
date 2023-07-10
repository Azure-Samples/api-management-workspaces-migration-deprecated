using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor;

public abstract class OperationHandler
{

    public readonly EntitiesRegistry registry;
    public abstract EntityType UsedEntities { get; }

    public abstract Type OperationType { get; }

    public OperationHandler(EntitiesRegistry registry = null)
    {
        this.registry = registry;
    }

    public abstract Task Handle(IMigrationOperation operation, string workspaceId);

    protected TOperation GetOperationOrThrow<TOperation>(IMigrationOperation operation)
        where TOperation : IMigrationOperation
    {
        if (operation.GetType() != this.OperationType)
            throw new ArgumentException("Operation is not the same as declared", nameof(operation));

        if ((operation.EntityType ^ this.UsedEntities) != 0)
            throw new ArgumentException("Operation entity is not the same as declared", nameof(operation));

        if (operation is not TOperation casted)
            throw new ArgumentException("Cast type is different then defined in Operation", nameof(TOperation));

        return casted;
    }

    protected bool tryGetNewEntity(ConnectOperation connectOperation, EntityType entityType, out Entity entity)
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
