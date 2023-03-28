using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies;

public class DependencyGraphBuilder
{
    private static readonly IReadOnlySet<EntityType> ApiInboundTypes = new HashSet<EntityType>()
        { EntityType.Product, EntityType.Tag, EntityType.NamedValue, EntityType.PolicyFragment, EntityType.VersionSet };

    private static readonly IReadOnlySet<EntityType> ProductInboundTypes = new HashSet<EntityType>()
        { EntityType.Tag, EntityType.NamedValue, EntityType.PolicyFragment };

    private readonly DependencyService dependencyService;

    public DependencyGraphBuilder(DependencyService dependencyService)
    {
        this.dependencyService = dependencyService;
    }

    public async Task<DependencyGraph> Build(IEnumerable<Entity> entities)
    {
        var graph = new DependencyGraph();
        var stack = new Stack<Entity>(entities);
        var visited = new HashSet<Entity>();

        while (stack.TryPop(out var entity))
        {
            if (!visited.Add(entity))
                continue;

            var dependencies = await this.dependencyService.ResolveDependencies(entity);
            var inboundFilter = InboundDependenciesFilter(entity.Type);
            var inbound = new List<Entity>();
            var outbound = new List<Entity>();
            foreach (var dependency in dependencies)
            {
                if (inboundFilter.Contains(dependency.Type))
                    inbound.Add(dependency);
                else
                    outbound.Add(dependency);

                stack.Push(dependency);
            }

            graph.SetRelation(entity, inbound, outbound);
        }

        return graph;
    }

    private static IReadOnlySet<EntityType> InboundDependenciesFilter(EntityType type) =>
        type switch
        {
            EntityType.VersionSet => ApiInboundTypes,
            EntityType.Api => ApiInboundTypes,
            EntityType.Product => ProductInboundTypes,
            // EntityType.ApiOperation => entity => false,
            // EntityType.Subscription => entity => false,
            // EntityType.Group => entity => false,
            // EntityType.PolicyFragment => entity => false,
            // EntityType.NamedValue => entity => false,
            // EntityType.Tag => entity => false,
            _ => new HashSet<EntityType>()
        };
}