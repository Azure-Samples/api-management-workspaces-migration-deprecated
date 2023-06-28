//using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Tags;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class TagsDependencyResolver : IEntityDependencyResolver
{
    private TagClient Client;

    public TagsDependencyResolver(TagClient client)
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
