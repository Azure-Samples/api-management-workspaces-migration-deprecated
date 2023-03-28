using System.Text.RegularExpressions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.TemplateModels;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class SubscriptionCopyHandler : OperationHandler
{
    private static readonly Regex ScopeRegex = new Regex("^(.+)/(products|apis)/([^/]+)$");
    
    private readonly SubscriptionClient subscriptionClient;
    private readonly EntitiesRegistry registry;

    public SubscriptionCopyHandler(SubscriptionClient subscriptionClient, EntitiesRegistry registry)
    {
        this.subscriptionClient = subscriptionClient;
        this.registry = registry;
    }

    public override EntityType UsedEntities => EntityType.Subscription;
    public override Type OperationType => typeof(CopyOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);

        if (copyOperation.Entity.ArmTemplate is not SubscriptionsTemplateResource resource) throw new Exception();

        var copy = resource.Serialize().Deserialize<SubscriptionsTemplateResource>();
        copy.Name = $"{copy.Name}-in-{workspaceId}";
        copy.Properties.scope = this.ModifyScope(copy.Properties.scope, workspaceId);
        copy.Type = null;
        var entity = new Entity(copy.Name, EntityType.Subscription, copy.Properties.displayName, copy);
        
        return this.subscriptionClient.Create(entity, workspaceId);
    }

    private string ModifyScope(string scope, string workspaceId)
    {
        var match = ScopeRegex.Match(scope);
        if (!match.Success) throw new Exception();
        var groups = match.Groups;
        var fake = new Entity(groups[3].Value, groups[2].Value == "apis" ? EntityType.Api : EntityType.Product);
        if (!this.registry.TryGetMapping(fake, out var newEntity))
            throw new Exception();
        
        return $"{groups[1]}/workspaces/{workspaceId}/{groups[2]}/{newEntity.Id}";
    }
}