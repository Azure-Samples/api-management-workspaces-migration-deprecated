using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class ApiOperationDependencyResolver : IEntityDependencyResolver
{
    private readonly PolicyRelatedDependenciesResolver policyDependenciesResolver;
    private readonly ApiClient apiClient;
    public ApiOperationDependencyResolver(ApiClient apiClient, PolicyRelatedDependenciesResolver policyDependenciesResolver)
    {
        this.apiClient = apiClient;
        this.policyDependenciesResolver = policyDependenciesResolver;
    }
    public EntityType Type => EntityType.ApiOperation;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (this.Type != entity.Type || entity is not OperationEntity opEntity) throw new Exception();

        var dependencies = new HashSet<Entity>();
        var policy = await this.apiClient.FetchOperationPolicy(opEntity.ApiId, opEntity.Id);
        if (policy != null)
        {
            dependencies.UnionWith(await this.policyDependenciesResolver.Resolve(policy));
        }

        dependencies.UnionWith(await this.apiClient.FetchOperationTags(opEntity.ApiId, opEntity.Id));

        return dependencies;
    }
}
