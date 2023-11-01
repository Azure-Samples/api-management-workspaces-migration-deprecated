using System.Text.RegularExpressions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.PolicyFragments;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Exceptions;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class PolicyRelatedDependenciesResolver: IPolicyRelatedDependenciesResolver
{
    public static readonly Regex IncludeFragmentFinder =
        new Regex("<include-fragment.+?fragment-id=\"(.+?)\".+?/>");

    public static readonly Regex IncludeSchemaFinder =
        new Regex("<content.+?schema-id=\"(.+?)\".+?/>");

    public static readonly Regex NamedValueFinder = new Regex("{{([^{}]*)}}");
    private static readonly Regex BackendFinder = new Regex("<set-backend-service.+?(backend-id|base-url)=\".+?\".+?\\/>");

    private readonly NamedValuesClient namedValuesClient;
    private readonly IPolicyFragmentClient policyFragmentsClient;
    private readonly ISchemasClient schemasClient;

    public PolicyRelatedDependenciesResolver(
        NamedValuesClient namedValuesClient,
        IPolicyFragmentClient policyFragmentsClient,
        ISchemasClient schemasClient
        )
    {
        this.namedValuesClient = namedValuesClient;
        this.policyFragmentsClient = policyFragmentsClient;
        this.schemasClient = schemasClient;
    }

    public async Task<IReadOnlyCollection<Entity>> Resolve(string policy)
    {
        var dependencies = new HashSet<Entity>();
        dependencies.UnionWith(await this.ResolvePolicyContent(policy));
        dependencies.UnionWith(await this.ResolvePolicyFragments(policy));
        dependencies.UnionWith(await this.ResolveSchemas(policy));
        return dependencies;
    }

    private async Task<IReadOnlyCollection<Entity>> ResolvePolicyContent(string policy)
    {
        this.ResolveBackends(policy);
        var dependencies = new HashSet<Entity>();
        dependencies.UnionWith(await this.ResolveNamedValues(policy));
        return dependencies;
    }

    private async Task<IReadOnlyCollection<Entity>> ResolvePolicyFragments(string policy)
    {
        var policyFragmentsNames = this.ExtractValuesWithRegex(policy, IncludeFragmentFinder);
        if (policyFragmentsNames.Count == 0)
        {
            return new List<Entity>(); 
        }
        else
        {
            var fragments = await this.policyFragmentsClient.Fetch(policyFragmentsNames);
            var dependencies = new HashSet<Entity>(fragments);
            foreach (var fragment in fragments)
            {
                dependencies.UnionWith(await this.ResolvePolicyContent(((PolicyFragmentsResource)fragment.ArmTemplate).Properties.Value));
            }
            return dependencies;
        }        
    }

    private Task<IReadOnlyCollection<Entity>> ResolveSchemas(string policy)
    {
        var schemasNames = this.ExtractValuesWithRegex(policy, IncludeSchemaFinder);
        return this.schemasClient.Fetch(schemasNames);
    }

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