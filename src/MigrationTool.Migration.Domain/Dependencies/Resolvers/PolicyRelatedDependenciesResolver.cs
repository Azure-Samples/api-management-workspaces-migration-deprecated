using System.Text.RegularExpressions;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class PolicyRelatedDependenciesResolver
{
    private readonly NamedValuesClient namedValuesClient;
    private readonly PolicyFragmentsClient policyFragmentsClient;

    public PolicyRelatedDependenciesResolver(NamedValuesClient namedValuesClient,
        PolicyFragmentsClient policyFragmentsClient)
    {
        this.namedValuesClient = namedValuesClient;
        this.policyFragmentsClient = policyFragmentsClient;
    }

    public async Task<IReadOnlyCollection<Entity>> Resolve(string policy)
    {
        var dependencies = new HashSet<Entity>();
        dependencies.UnionWith(await this.ResolveNamedValues(policy));
        dependencies.UnionWith(await this.ResolvePolicyFragments(policy));
        return dependencies;
    }

    private Task<IReadOnlyCollection<Entity>> ResolvePolicyFragments(string policy)
    {
        var policyFragmentsNames = this.ExtractPolicyFragmentNames(policy);
        return policyFragmentsNames.Count != 0
            ? this.policyFragmentsClient.Fetch(policyFragmentsNames)
            : Task.FromResult<IReadOnlyCollection<Entity>>(new List<Entity>());
    }

    private IReadOnlyCollection<string> ExtractPolicyFragmentNames(string policy)
    {
        var includeFragmentFinder = new Regex("<include-fragment\\s*fragment-id=\"([^\"]*)\"\\s*/>");
        var matchCollection = includeFragmentFinder.Matches(policy);
        return matchCollection.Select(_ => _.Groups[1].Value).ToHashSet();
    }

    private Task<IReadOnlyCollection<Entity>> ResolveNamedValues(string policy)
    {
        var namedValuesNames = this.ExtractNamedValuesNames(policy);
        return namedValuesNames.Count != 0
            ? this.namedValuesClient.Fetch(namedValuesNames)
            : Task.FromResult<IReadOnlyCollection<Entity>>(new List<Entity>());
    }

    private IReadOnlyCollection<string> ExtractNamedValuesNames(string policy)
    {
        var namedValueFinder = new Regex("{{([^{}]*)}}");
        var matchCollection = namedValueFinder.Matches(policy);
        return matchCollection.Select(_ => _.Groups[1].Value).ToHashSet();
    }
}