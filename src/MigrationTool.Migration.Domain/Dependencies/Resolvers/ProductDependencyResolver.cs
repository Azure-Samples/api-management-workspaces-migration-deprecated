using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class ProductDependencyResolver : IEntityDependencyResolver
{
    private readonly IProductClient productClient;
    private readonly ISubscriptionClient subscriptionClient;

    private readonly IPolicyRelatedDependenciesResolver policyDependenciesResolver;

    public ProductDependencyResolver(IProductClient productClient,
        ISubscriptionClient subscriptionClient,
        IPolicyRelatedDependenciesResolver policyDependenciesResolver)
    {
        this.productClient = productClient;
        this.subscriptionClient = subscriptionClient;
        this.policyDependenciesResolver = policyDependenciesResolver;
    }

    public EntityType Type => EntityType.Product;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (this.Type != entity.Type) throw new Exception();

        var dependencies = new HashSet<Entity>();

        dependencies.UnionWith(await this.ResolveApis(entity));
        dependencies.UnionWith(await this.ResolveTags(entity));
        dependencies.UnionWith(await this.ResolveGroups(entity));
        dependencies.UnionWith(await this.ResolveSubscriptions(entity));
        dependencies.UnionWith(await this.ResolvePolicyRelatedDependencies(entity));

        return dependencies;
    }


    private Task<IReadOnlyCollection<Entity>> ResolveApis(Entity entity)
        => this.productClient.FetchApis(entity.Id);

    private Task<IReadOnlyCollection<Entity>> ResolveTags(Entity entity)
        => this.productClient.FetchTags(entity.Id);

    private Task<IReadOnlyCollection<Entity>> ResolveGroups(Entity entity)
        => this.productClient.FetchGroups(entity.Id);

    private Task<IReadOnlyCollection<Entity>> ResolveSubscriptions(Entity entity)
        => this.subscriptionClient.FetchForProduct(entity.Id);

    private async Task<IReadOnlyCollection<Entity>> ResolvePolicyRelatedDependencies(Entity entity)
    {
        var policy = await this.productClient.FetchPolicy(entity.Id);
        if (policy != null)
            return await this.policyDependenciesResolver.Resolve(policy);
        return new List<Entity>();
    }
}