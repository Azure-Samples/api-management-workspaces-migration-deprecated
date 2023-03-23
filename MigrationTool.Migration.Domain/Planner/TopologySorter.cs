using System.Collections;
using MigrationTool.Migration.Domain.Dependencies;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Planner;

public class TopologySorter
{
    private readonly Dictionary<Entity, bool> marks = new();
    private readonly Stack<Entity> sortedEntities = new();

    private readonly DependencyGraph graph;

    public TopologySorter(DependencyGraph graph)
    {
        this.graph = graph;
    }

    public ICollection<Entity> Sort()
    {
        foreach (var node in this.graph.Nodes)
        {
            if (!this.marks.TryGetValue(node, out var visited) || !visited)
                this.Visit(node);
        }

        return this.sortedEntities.ToList();
    }

    private void Visit(Entity node)
    {
        var containsMark = this.marks.TryGetValue(node, out var visited);

        if (containsMark && visited)
            return;

        if (containsMark && !visited)
            throw new Exception("Cycle detected");

        this.marks[node] = false;

        foreach (var nextNode in this.graph.Outbounds(node))
        {
            this.Visit(nextNode);
        }

        this.marks[node] = true;
        this.sortedEntities.Push(node);
    }
}