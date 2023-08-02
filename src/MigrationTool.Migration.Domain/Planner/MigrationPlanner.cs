using MigrationTool.Migration.Domain.Dependencies;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Planner;

public static class MigrationPlanner
{
    public static MigrationPlan Plan(DependencyGraph graph, MigrationType type)
    {
        var sortedEntities = new TopologySorter(graph).Sort();
        var plan = new MigrationPlan();

        AddCopyAndConnectOperations(graph, sortedEntities, plan);

        if (type == MigrationType.Move)
        {
            AddMoveAndDeleteOperations(sortedEntities, plan);
        }

        return plan;
    }

    private static void AddMoveAndDeleteOperations(ICollection<Entity> sortedEntities, MigrationPlan plan)
    {
        foreach (var entity in sortedEntities.Reverse())
        {
            plan.AddDeleteOperation(entity);
        }

        foreach (var entity in sortedEntities)
        {
            plan.AddRenameOperation(entity);
        }
    }

    private static void AddCopyAndConnectOperations(DependencyGraph graph,
        ICollection<Entity> sortedEntities,
        MigrationPlan plan)
    {
        foreach (var entity in sortedEntities)
        {
            if(ShouldCopy(entity))
            {
                plan.AddCopyOperation(entity);
            }
            foreach (var connections in graph.Inbounds(entity))
            {
                if(ShouldConnect(entity, connections))
                {
                    plan.AddConnectOperation(connections, entity);
                }
            }
        }
    }

    private static bool ShouldCopy(Entity entity)
    {
        return entity.Type != EntityType.ApiOperation && entity.Type != EntityType.User;
    }

    private static bool ShouldConnect(Entity entity, Entity connections)
    {
        return !((entity.Type == EntityType.Api && connections.Type == EntityType.ApiOperation) 
            || (entity.Type == EntityType.ApiOperation && connections.Type == EntityType.Api));
    }
}