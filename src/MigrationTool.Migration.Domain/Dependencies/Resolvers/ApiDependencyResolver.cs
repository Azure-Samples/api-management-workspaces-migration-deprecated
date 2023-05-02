using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Exceptions;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class ApiDependencyResolver : IEntityDependencyResolver
{
    private readonly ApiClient apiClient;
    private readonly SubscriptionClient subscriptionClient;
    private readonly VersionSetClient versionSetClient;
    private readonly GatewayClient gatewayClient;

    private readonly PolicyRelatedDependenciesResolver policyDependenciesResolver;

    public ApiDependencyResolver(ApiClient apiClient,
        SubscriptionClient subscriptionClient,
        PolicyRelatedDependenciesResolver policyDependenciesResolver,
        VersionSetClient versionSetClient,
        GatewayClient gatewayClient)
    {
        this.apiClient = apiClient;
        this.subscriptionClient = subscriptionClient;
        this.policyDependenciesResolver = policyDependenciesResolver;
        this.versionSetClient = versionSetClient;
        this.gatewayClient = gatewayClient;
    }

    public EntityType Type => EntityType.Api;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (this.Type != entity.Type || entity is not ApiEntity apiEntity) throw new Exception();

        if (await this.gatewayClient.IsLinkedWithGateway((ApiEntity)entity))
        {
            throw new EntityNotSupportedException("gateway");
        }

        var dependencies = new HashSet<Entity>();

        dependencies.UnionWith(await this.ResolvePolicyRelatedDependencies(apiEntity));
        dependencies.UnionWith(await this.ResolveProducts(apiEntity));
        dependencies.UnionWith(await this.ResolveTags(apiEntity));
        dependencies.UnionWith(await this.ResolveApiOperationsRelatedDependencies(apiEntity));
        dependencies.UnionWith(await this.ResolveSubscriptions(apiEntity));
        dependencies.UnionWith(await this.ResolveRevisionDependencies(apiEntity));
        dependencies.UnionWith(await this.ResolveVersionSetDependencies(apiEntity));

        return dependencies;
    }

    async Task<IEnumerable<Entity>> ResolveRevisionDependencies(ApiEntity entity)
    {
        var dependencies = new HashSet<Entity>();
        foreach (var revision in entity.Revisions)
        {
            dependencies.UnionWith(await this.ResolvePolicyRelatedDependencies(revision));
            dependencies.UnionWith(await this.ResolveTags(revision));
            dependencies.UnionWith(await this.ResolveApiOperationsRelatedDependencies(revision));
        }

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

    async Task<IReadOnlyCollection<Entity>> ResolveVersionSetDependencies(Entity entity)
    {
        var versionSet = await this.versionSetClient.FetchVersionSet(entity);
        if (versionSet != null)
            return new List<Entity>() { versionSet };
        return new List<Entity>();
    }
}