
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class ApiVersionSetDependencyResolver : IEntityDependencyResolver
{
    public EntityType Type => EntityType.VersionSet;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        return ((VersionSetEntity) entity).Apis;
    }
}
