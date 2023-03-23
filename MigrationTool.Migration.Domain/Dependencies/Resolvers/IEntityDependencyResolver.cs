using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public interface IEntityDependencyResolver
{
    public EntityType Type { get; }

    public Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity);
}