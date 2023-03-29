
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class ApiVersionSetDependencyResolver : IEntityDependencyResolver
{
    private ApiDependencyResolver ApiDependencyResolver;
    public ApiVersionSetDependencyResolver(ApiDependencyResolver apiDependencyResolver) { 
        this.ApiDependencyResolver = apiDependencyResolver;
    }

    public EntityType Type => EntityType.VersionSet;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        List<Entity> dependencies = new List<Entity>();

        foreach(var api in ((VersionSetEntity)entity).Apis ) {

            var apiDependencies = await this.ApiDependencyResolver.ResolveDependencies(api);
            dependencies.AddRange(apiDependencies);
        }

        return dependencies.ToHashSet();
    }
}
