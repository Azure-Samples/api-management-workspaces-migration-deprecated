using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Tags;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;
using MigrationTool.Migration.Domain.Extensions;
using MigrationTool.Migration.Domain.Clients.Abstraction;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class TagCopyHandler : OperationHandler
{
    private ITagClient tagClient;
    public TagCopyHandler(ITagClient tagClient, EntitiesRegistry registry) : base (registry)
    {
        this.tagClient = tagClient;
    }
    public override EntityType UsedEntities => EntityType.Tag;

    public override Type OperationType => typeof(CopyOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);
        var operationTemplate = (TagTemplateResource)copyOperation.Entity.ArmTemplate;
        var newTemplate = ModifyTemplate(workspaceId, operationTemplate);

        var entity = new Entity(newTemplate.Name,
            EntityType.NamedValue,
            newTemplate.Properties.DisplayName,
            newTemplate);

        this.registry.RegisterMapping(operation.Entity, entity);

        return this.tagClient.Create(newTemplate, workspaceId);
    }

    private TagTemplateResource ModifyTemplate(string workspaceId, TagTemplateResource tagTemplate)
    {
        var newTemplate = tagTemplate.Copy();
        newTemplate.Name = $"{newTemplate.Name}-in-{workspaceId}";
        newTemplate.Properties.DisplayName = $"{newTemplate.Properties.DisplayName}-in-{workspaceId}";
        return newTemplate;
    }
}
