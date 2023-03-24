using MigrationTool.Migration.Domain.Dependencies.Resolvers;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies;

public class DependencyService
{
    private readonly IReadOnlyDictionary<EntityType, IEntityDependencyResolver> resolvers;

    public DependencyService(IEnumerable<IEntityDependencyResolver> resolvers)
    {
        this.resolvers = resolvers.ToDictionary(_ => _.Type);
    }

    public Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (!this.resolvers.TryGetValue(entity.Type, out var resolver))
        {
            throw new Exception($"Resolver of type {entity.Type} is not registered");
        }

        return resolver.ResolveDependencies(entity);
    }
}