using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Exceptions;
using MigrationTool.Migration.Domain.Extensions;
using System.Configuration;

namespace MigrationTool.Migration.Domain.Dependencies;

public class DependencyGraphBuilder
{
    private static readonly IReadOnlySet<EntityType> ApiInboundTypes = new HashSet<EntityType>()
        { EntityType.Product, EntityType.Tag, EntityType.NamedValue, EntityType.PolicyFragment };

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

            IReadOnlyCollection<Entity> dependencies = new List<Entity>();

            try
            {
                dependencies = await this.dependencyService.ResolveDependencies(entity);
            } catch (EntityNotSupportedException ex)
            {
                var msg = ConfigurationManager.AppSettings["entityNotSupported"];
                msg = msg.Replace("{type}", entity.Type.GetDescription());
                msg = msg.Replace("{displayName}", entity.DisplayName);
                msg = msg.Replace("{notSupproted}", ex.Message);
                Console.WriteLine(msg);
                return null;
            }

            if (dependencies.Where(dependency => dependency.Type.Equals(EntityType.Api)).Except(entities).Any())
            {
                //skip entities refering to the not selected APIs
                continue;
            }



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