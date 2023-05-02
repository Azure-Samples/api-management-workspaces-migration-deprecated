using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Exceptions;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class ProductDependencyResolver : IEntityDependencyResolver
{
    private readonly ProductClient productClient;
    private readonly SubscriptionClient subscriptionClient;

    private readonly PolicyRelatedDependenciesResolver policyDependenciesResolver;

    public ProductDependencyResolver(ProductClient productClient,
        SubscriptionClient subscriptionClient,
        PolicyRelatedDependenciesResolver policyDependenciesResolver)
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