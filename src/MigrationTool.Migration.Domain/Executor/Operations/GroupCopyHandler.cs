using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Groups;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.NamedValues;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ProductApis;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class GroupCopyHandler : OperationHandler
{
    private readonly IGroupsClient _groupsClient;
    private readonly EntitiesRegistry _registry;
    public GroupCopyHandler(IGroupsClient groupsClient, EntitiesRegistry registry) : base(registry)
    {
        this._groupsClient = groupsClient;
        this._registry = registry;
    }
    public override EntityType UsedEntities => EntityType.Group;

    public override Type OperationType => typeof(CopyOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {

        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);
        var groupTemplate = (GroupTemplateResource)copyOperation.Entity.ArmTemplate;
        var newTemplate = ModifyTemplate(workspaceId, groupTemplate);

        var entity = new Entity(newTemplate.Name,
            EntityType.Group,
            newTemplate.Properties.DisplayName,
            newTemplate);
        this._registry.RegisterMapping(operation.Entity, entity);

        return this._groupsClient.Create(newTemplate, workspaceId);
    }

    static GroupTemplateResource ModifyTemplate(string workspaceId, GroupTemplateResource template)
    {
        var groupTemplate = template.Copy();
        groupTemplate.Name = $"{groupTemplate.Name}-in-{workspaceId}";
        groupTemplate.Properties.DisplayName = $"{groupTemplate.Properties.DisplayName}-in-{workspaceId}";
        return groupTemplate;
    }
}
