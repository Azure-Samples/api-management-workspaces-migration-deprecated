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

    public Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        return this._groupsClient.FetchEntities(entity.Id);
    }
}
