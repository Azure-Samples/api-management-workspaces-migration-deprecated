using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class ApiVersionSetDependencyResolver : IEntityDependencyResolver
{
    public EntityType Type => EntityType.VersionSet;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (this.Type != entity.Type || entity is not VersionSetEntity versionSetEntity) throw new Exception();

        return versionSetEntity.Apis;
    }
}