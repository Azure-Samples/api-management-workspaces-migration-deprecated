using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class NamedValueDependencyResolver : IEntityDependencyResolver
{
    // private static readonly Regex globalRegex = new Regex("^/policies/policy$");
    private static readonly Regex apiRegex = new Regex("^/apis/(.+);rev=\\d+/policies/policy$");
    private static readonly Regex apiOperationRegex = new Regex("^/apis/(.+);rev=\\d+/operations/(.+)/policies/policy$");
    private static readonly Regex productRegex = new Regex("^/product/(.+)/policies/policy$");
    // private static readonly Regex workspaceRegex = new Regex("^/workspaces/(.+)/policies/policy$");

    private readonly NamedValuesClient namedValuesClient;

    public NamedValueDependencyResolver(NamedValuesClient namedValuesClient)
    {
        this.namedValuesClient = namedValuesClient;
    }

    public EntityType Type => EntityType.NamedValue;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (this.Type != entity.Type) throw new Exception();

        var ids = await this.namedValuesClient.FetchReferenceIds(entity.Id);
        
        var result = new List<Entity>();
        foreach (var id in ids)
        {
            if (this.TryMatchEntity(id, out var dependency))
            {
                result.Add(dependency);
            }
        }
        
        return result;
    }

    private bool TryMatchEntity(string id, [MaybeNullWhen(false)] out Entity entity)
    {
        // var match = globalRegex.Match(id);
        // if (match.Success)
        // {
        //     entity = new Entity("", EntityType.Global);
        //     return true;
        // }
        //
        // match = workspaceRegex.Match(id);
        // if (match.Success)
        // {
        //     entity = new Entity(match.Groups[1].Value, EntityType.Workspace);
        //     return true;
        // }
        
        var match = apiOperationRegex.Match(id);
        if (match.Success)
        {
            entity = new Entity(match.Groups[1].Value, EntityType.Api);
            return true;
        }
        
        match = apiRegex.Match(id);
        if (match.Success)
        {
            entity = new Entity(match.Groups[1].Value, EntityType.Api);
            return true;
        }
        
        match = productRegex.Match(id);
        if (match.Success)
        {
            entity = new Entity(match.Groups[1].Value, EntityType.Product);
            return true;
        }
        
        entity = null;
        return false;
        
    }
}