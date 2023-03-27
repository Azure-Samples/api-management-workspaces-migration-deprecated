using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies;

public class DependencyGraph
{
    private readonly  Dictionary<Entity, HashSet<Entity>> inbound = new Dictionary<Entity, HashSet<Entity>>();
    private readonly  Dictionary<Entity, HashSet<Entity>> outbound = new Dictionary<Entity, HashSet<Entity>>();

    private readonly  HashSet<Entity> noInboundNodes = new HashSet<Entity>();

    internal void SetRelation(Entity entity, IReadOnlyCollection<Entity> inboundDependency, IReadOnlyCollection<Entity> outboundDependency)
    {
        var currentInbound = this.inbound.TryGetOrNew(entity);
        currentInbound.UnionWith(inboundDependency);

        var currentOutbound = this.outbound.TryGetOrNew(entity);
        currentOutbound.UnionWith(outboundDependency);

        if (inboundDependency.Count == 0)
            this.noInboundNodes.Add(entity);

        inboundDependency.ForEachAddDependency(this.outbound, entity);
        outboundDependency.ForEachAddDependency(this.inbound, entity);
    }

    public IReadOnlySet<Entity> Nodes => this.inbound.Keys.ToHashSet();

    public IReadOnlySet<Entity> NodesWithoutInbound => this.noInboundNodes;

    public IReadOnlyCollection<Entity> Inbounds(Entity entity) =>
        this.inbound.TryGetValue(entity, out var inbound) ? inbound : new List<Entity>();

    public IReadOnlyCollection<Entity> Outbounds(Entity entity) =>
        this.outbound.TryGetValue(entity, out var outbound) ? outbound : new List<Entity>();

    public override string ToString()
    {
        return $"{nameof(this.inbound)}: {string.Join(", ", this.inbound.Keys)}";
    }
}

static class LocalExtensions
{
    public static HashSet<Entity> TryGetOrNew(this Dictionary<Entity, HashSet<Entity>> dictionary, Entity entity)
    {
        if (dictionary.TryGetValue(entity, out var set)) 
            return set;
        
        set = new HashSet<Entity>();
        dictionary.Add(entity, set);
        return set;
    }
    
    public static void ForEachAddDependency(this IReadOnlyCollection<Entity> collection,
        Dictionary<Entity, HashSet<Entity>> dictionary,
        Entity dependency)
    {
        foreach (var entity in collection)
        {
            var entities = dictionary.TryGetOrNew(entity);
            entities.Add(dependency);
        }
    }
}