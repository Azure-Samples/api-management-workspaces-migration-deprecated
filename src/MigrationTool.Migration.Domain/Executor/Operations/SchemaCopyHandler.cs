using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Schemas;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class SchemaCopyHandler : OperationHandler
{
    private readonly ISchemasClient schemasClient;
    private readonly EntitiesRegistry _registry;
    public SchemaCopyHandler(ISchemasClient schemasClient, EntitiesRegistry registry) : base(registry)
    {
        this.schemasClient = schemasClient;
        this._registry = registry;
    }

    public override EntityType UsedEntities => EntityType.Schema;

    public override Type OperationType => typeof(CopyOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        //PolicyFragmentsResource
        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);
        var schemaTemplate = (SchemaTemplateResource)copyOperation.Entity.ArmTemplate;
        var newTemplate = ModifyTemplate(workspaceId, schemaTemplate);

        var entity = new Entity(newTemplate.Name,
            EntityType.Schema,
            newTemplate.Name,
            newTemplate);
        this._registry.RegisterMapping(operation.Entity, entity);

        return this.schemasClient.Create(newTemplate, workspaceId);
    }

    static SchemaTemplateResource ModifyTemplate(string workspaceId, SchemaTemplateResource template)
    {
        var newTemplate = template.Copy();
        newTemplate.Name = $"{newTemplate.Name}-in-{workspaceId}";
        return newTemplate;
    }
}
