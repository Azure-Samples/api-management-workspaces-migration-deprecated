using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Planner;

namespace MigrationTool.Migration.Domain.Executor;

public class MigrationPlanExecutor
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<EntityType, OperationHandler>> handlers;

    public MigrationPlanExecutor(IEnumerable<OperationHandler> handlers)
    {
        this.handlers = handlers.GroupBy(_ => _.OperationType)
            .ToDictionary<IGrouping<Type, OperationHandler>, Type, IReadOnlyDictionary<EntityType, OperationHandler>>(
                _ => _.Key, _ => _.ToDictionary(__ => __.UsedEntities));
    }

    public async Task Execute(MigrationPlan plan, string workspaceId)
    {
        foreach (var operation in plan.Operations)
        {
            if (!this.handlers.TryGetValue(operation.GetType(), out var operationHandlers))
                throw new Exception($"No handler for operation {operation.GetType().Name}");
            
            if (!operationHandlers.TryGetValue(operation.EntityType, out var handler))
                throw new Exception($"No handler for operation {operation.GetType().Name} for entity {operation.Entity.Type}");

            await handler.Handle(operation, workspaceId);
        }
    }
}