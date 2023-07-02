using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class TagsDependencyResolver : ITagsDependencyResolver
{
    private ITagClient Client;

    public TagsDependencyResolver(ITagClient client)
    {
        this.Client = client;
    }
    
    public EntityType Type => EntityType.Tag;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (this.Type != entity.Type) throw new Exception();

        var dependencies = new HashSet<Entity>();

        dependencies.UnionWith(await this.Resolve(entity));
        return dependencies;
    }

    private Task<IReadOnlyCollection<Entity>> Resolve(Entity entity)
    {
        return this.Client.FetchEntities(entity.Id);
    }
}
