using System.Text.RegularExpressions;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Exceptions;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class PolicyRelatedDependenciesResolver: IPolicyRelatedDependenciesResolver
{
    private static readonly Regex IncludeFragmentFinder =
        new Regex("<include-fragment.+?fragment-id=\"(.+?)\".+?/>");

    private static readonly Regex NamedValueFinder = new Regex("{{([^{}]*)}}");
    private static readonly Regex BackendFinder = new Regex("<set-backend-service.+?(backend-id|base-url)=\".+?\".+?\\/>");

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
        this.ResolveBackends(policy);
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

    private IReadOnlyCollection<string> ExtractPolicyFragmentNames(string policy) =>
        IncludeFragmentFinder.Matches(policy).Select(_ => _.Groups[1].Value).ToHashSet();

    private void ResolveBackends(string policy)
    {
        var backEnds = this.ExtractValuesWithRegex(policy, BackendFinder);
        if (backEnds.Count != 0)
        {
            throw new EntityNotSupportedException("backend");
        }
    }
    private Task<IReadOnlyCollection<Entity>> ResolveNamedValues(string policy)
    {
        var namedValuesNames = this.ExtractValuesWithRegex(policy, NamedValueFinder);
        return namedValuesNames.Count != 0
            ? this.namedValuesClient.Fetch(namedValuesNames)
            : Task.FromResult<IReadOnlyCollection<Entity>>(new List<Entity>());
    }

    private IReadOnlyCollection<string> ExtractValuesWithRegex(string policy, Regex regex) =>
        regex.Matches(policy).Select(_ => _.Groups[1].Value).ToHashSet();
}