using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class NoDependencyResolver : IEntityDependencyResolver
{
    public EntityType Type { get; }
    
    public NoDependencyResolver(EntityType type)
    {
        this.Type = type;
    }

    public Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (this.Type != entity.Type) throw new Exception();

        return Task.FromResult<IReadOnlyCollection<Entity>>(new List<Entity>());
    }
}