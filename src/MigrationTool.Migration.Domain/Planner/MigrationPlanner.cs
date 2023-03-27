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
            plan.AddCopyOperation(entity);
            foreach (var connections in graph.Inbounds(entity))
            {
                plan.AddConnectOperation(connections, entity);
            }
        }
    }
}