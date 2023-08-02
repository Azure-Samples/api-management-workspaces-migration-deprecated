using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class GroupDependencyResolver : IEntityDependencyResolver
{
    private IGroupsClient _groupsClient;
    public GroupDependencyResolver(IGroupsClient groupsClient) 
    { 
        this._groupsClient = groupsClient;
    }
    public EntityType Type => EntityType.Group;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        var dependencies = new HashSet<Entity>();
        var products = this._groupsClient.FetchProducts(entity.Id);
        var users = this._groupsClient.FetchUsers(entity.Id);

        dependencies.UnionWith(await products);
        dependencies.UnionWith(await users);

        return dependencies;
    }
}
