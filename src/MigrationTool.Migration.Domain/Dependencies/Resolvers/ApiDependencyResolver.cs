using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class ApiDependencyResolver : IEntityDependencyResolver
{
    private readonly ApiClient apiClient;
    private readonly SubscriptionClient subscriptionClient;

    private readonly PolicyRelatedDependenciesResolver policyDependenciesResolver;

    public ApiDependencyResolver(ApiClient apiClient,
        SubscriptionClient subscriptionClient,
        PolicyRelatedDependenciesResolver policyDependenciesResolver)
    {
        this.apiClient = apiClient;
        this.subscriptionClient = subscriptionClient;
        this.policyDependenciesResolver = policyDependenciesResolver;
    }

    public EntityType Type => EntityType.Api;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (this.Type != entity.Type) throw new Exception();

        var dependencies = new HashSet<Entity>();

        dependencies.UnionWith(await this.ResolveProducts(entity));
        dependencies.UnionWith(await this.ResolveTags(entity));
        dependencies.UnionWith(await this.ResolveApiOperationsRelatedDependencies(entity));
        dependencies.UnionWith(await this.ResolvePolicyRelatedDependencies(entity));
        dependencies.UnionWith(await this.ResolveSubscriptions(entity));

        return dependencies;
    }

    Task<IReadOnlyCollection<Entity>> ResolveSubscriptions(Entity entity) =>
        this.subscriptionClient.FetchForApi(entity.Id);

    Task<IReadOnlyCollection<Entity>> ResolveProducts(Entity entity) =>
        this.apiClient.FetchProducts(entity.Id);

    Task<IReadOnlyCollection<Entity>> ResolveTags(Entity entity) =>
        this.apiClient.FetchTags(entity.Id);

    async Task<IReadOnlyCollection<Entity>> ResolveApiOperationsRelatedDependencies(Entity entity)
    {
        var dependencies = new HashSet<Entity>();
        var operations = await this.apiClient.FetchOperations(entity.Id);

        foreach (var operation in operations)
        {
            var policy = await this.apiClient.FetchOperationPolicy(entity.Id, operation.Id);
            if (policy != null)
                dependencies.UnionWith(await this.policyDependenciesResolver.Resolve(policy));
            dependencies.UnionWith(await this.apiClient.FetchOperationTags(entity.Id, operation.Id));
        }

        return dependencies;
    }

    async Task<IReadOnlyCollection<Entity>> ResolvePolicyRelatedDependencies(Entity entity)
    {
        var policy = await this.apiClient.FetchPolicy(entity.Id);
        if (policy != null)
            return await this.policyDependenciesResolver.Resolve(policy);
        return new List<Entity>();
    }
}