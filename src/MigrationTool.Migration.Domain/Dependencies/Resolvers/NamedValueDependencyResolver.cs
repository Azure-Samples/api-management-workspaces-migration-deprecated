using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public class NamedValueDependencyResolver : IEntityDependencyResolver
{
    // private static readonly Regex globalRegex = new Regex("^/policies/policy$");
    private static readonly Regex apiRegex = new Regex("^/apis/(.+);rev=(.+)/policies/policy$");
    private static readonly Regex apiOperationRegex = new Regex("^/apis/(.+);rev=(.+)/operations/(.+)/policies/policy$");
    private static readonly Regex productRegex = new Regex("^/products/(.+)/policies/policy$");
    // private static readonly Regex workspaceRegex = new Regex("^/workspaces/(.+)/policies/policy$");

    private readonly NamedValuesClient namedValuesClient;
    private readonly IApiClient apiClient;

    public NamedValueDependencyResolver(NamedValuesClient namedValuesClient, IApiClient apiClient)
    {
        this.namedValuesClient = namedValuesClient;
        this.apiClient = apiClient;
    }

    public EntityType Type => EntityType.NamedValue;

    public async Task<IReadOnlyCollection<Entity>> ResolveDependencies(Entity entity)
    {
        if (this.Type != entity.Type) throw new Exception();

        var ids = await this.namedValuesClient.FetchReferenceIds(entity.Id);
        
        var result = new List<Entity>();
        foreach (var id in ids)
        {
            var dependency = await this.TryMatchEntity(id);
            if (dependency != null)
            {
                result.Add(dependency);
            }
        }
        
        return result;
    }

    private async Task<Entity> TryMatchEntity(string id)
    {
        Entity entity = null;
        
        var match = apiOperationRegex.Match(id);
        if (match.Success)
        {
            return await this.getOperationById(match.Groups[1].Value, match.Groups[3].Value);
        }
        
        match = apiRegex.Match(id);
        if (match.Success)
        {
            return await this.getApiById(match.Groups[1].Value);
        }
        
        match = productRegex.Match(id);
        if (match.Success)
        {
            entity = new Entity(match.Groups[1].Value, EntityType.Product);
            return entity;
        }
        
        return entity;
        
    }

    private async Task<Entity> getApiById(string id)
    {
        return await this.apiClient.Fetch(id);
    }

    private async Task<Entity> getOperationById(string apiId, string operationId)
    {
        var operations = await this.apiClient.FetchOperations(apiId);
        return operations.First(operation => operation.Id == operationId);
    }
}