using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.NamedValues;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class NamedValueCopyHandler : OperationHandler
{
    private readonly NamedValuesClient namedValuesClient;
    private readonly EntitiesRegistry registry;

    public NamedValueCopyHandler(NamedValuesClient namedValuesClient, EntitiesRegistry registry)
    {
        this.namedValuesClient = namedValuesClient;
        this.registry = registry;
    }

    public override EntityType UsedEntities => EntityType.NamedValue;
    public override Type OperationType => typeof(CopyOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);
        var namedValueTemplate = (NamedValueTemplateResource)copyOperation.Entity.ArmTemplate;
        var newTemplate = ModifyTemplate(workspaceId, namedValueTemplate);

        var entity = new Entity(newTemplate.Name,
            EntityType.NamedValue,
            newTemplate.Properties.DisplayName,
            newTemplate);
        this.registry.RegisterMapping(operation.Entity, entity);
        
        return this.namedValuesClient.Create(newTemplate, workspaceId);
    }

    static NamedValueTemplateResource ModifyTemplate(string workspaceId, NamedValueTemplateResource namedValueTemplate)
    {
        var newTemplate = namedValueTemplate.Copy();
        newTemplate.Name = $"{newTemplate.Name}-in-{workspaceId}";
        newTemplate.Properties.DisplayName = $"{newTemplate.Properties.DisplayName}-in-{workspaceId}";
        return newTemplate;
    }
}