using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Exceptions;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class ApiDependencyResolver : IEntityDependencyResolver
{
    private readonly IApiClient apiClient;
    private readonly ISubscriptionClient subscriptionClient;
    private readonly IVersionSetClient versionSetClient;
    private readonly IGatewayClient gatewayClient;

    private readonly IPolicyRelatedDependenciesResolver policyDependenciesResolver;
    private readonly ITagsDependencyResolver tagsDependencyResolver;

    public ApiDependencyResolver(IApiClient apiClient,
        ISubscriptionClient subscriptionClient,
        IPolicyRelatedDependenciesResolver policyDependenciesResolver,
        IVersionSetClient versionSetClient,
        ITagsDependencyResolver tagsDependencyResolver,
        IGatewayClient gatewayClient)
    {
        this.apiClient = apiClient;
        this.subscriptionClient = subscriptionClient;
        this.policyDependenciesResolver = policyDependenciesResolver;
        this.versionSetClient = versionSetClient;
        this.gatewayClient = gatewayClient;
        this.tagsDependencyResolver = tagsDependencyResolver;
    }

    public EntityType Type => EntityType.Api;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (this.Type != entity.Type || entity is not ApiEntity apiEntity) throw new Exception();

        if (await this.gatewayClient.IsLinkedWithGateway(apiEntity))
        {
            throw new EntityNotSupportedException("gateway");
        }

        var dependencies = new HashSet<Entity>();

        dependencies.UnionWith(await this.ResolvePolicyRelatedDependencies(apiEntity));
        dependencies.UnionWith(await this.ResolveProducts(apiEntity));
        dependencies.UnionWith(await this.ResolveTags(apiEntity));
        dependencies.UnionWith(await this.ResolveApiOperations(apiEntity));
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
            dependencies.UnionWith(await this.ResolveApiOperations(revision));
        }

        return dependencies;
    }

    Task<IReadOnlyCollection<Entity>> ResolveSubscriptions(Entity entity) =>
        this.subscriptionClient.FetchForApi(entity.Id);

    Task<IReadOnlyCollection<Entity>> ResolveProducts(Entity entity) =>
        this.apiClient.FetchProducts(entity.Id);

    Task<IReadOnlyCollection<Entity>> ResolveTags(Entity entity) =>
        this.apiClient.FetchTags(entity.Id);

    async Task<IReadOnlyCollection<Entity>> ResolveApiOperations(Entity entity)
    {
        return await this.apiClient.FetchOperations(entity.Id);
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