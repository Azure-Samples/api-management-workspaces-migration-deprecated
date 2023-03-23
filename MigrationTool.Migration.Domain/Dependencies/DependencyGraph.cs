using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies;

public class DependencyGraph
{
    private readonly  Dictionary<Entity, IReadOnlyList<Entity>> inbound = new Dictionary<Entity, IReadOnlyList<Entity>>();
    private readonly  Dictionary<Entity, IReadOnlyList<Entity>> outbound = new Dictionary<Entity, IReadOnlyList<Entity>>();

    private readonly  HashSet<Entity> noInboundNodes = new HashSet<Entity>();

    internal void SetRelation(Entity entity, IReadOnlyList<Entity> inbound, IReadOnlyList<Entity> outbound)
    {
        this.inbound.Add(entity, inbound);
        this.outbound.Add(entity, outbound);

        if (inbound.Count == 0)
            this.noInboundNodes.Add(entity);
    }

    public IReadOnlySet<Entity> Nodes => this.inbound.Keys.ToHashSet();

    public IReadOnlySet<Entity> NodesWithoutInbound => this.noInboundNodes;

    public IReadOnlyList<Entity> Inbounds(Entity entity) =>
        this.inbound.TryGetValue(entity, out var inbound) ? inbound : new List<Entity>();

    public IReadOnlyList<Entity> Outbounds(Entity entity) =>
        this.outbound.TryGetValue(entity, out var outbound) ? outbound : new List<Entity>();

    public override string ToString()
    {
        return $"{nameof(this.inbound)}: {string.Join(", ", this.inbound.Keys)}";
    }
}