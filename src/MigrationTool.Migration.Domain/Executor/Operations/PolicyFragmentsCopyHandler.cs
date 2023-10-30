using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.PolicyFragments;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class PolicyFragmentsCopyHandler: OperationHandler
{
    private readonly IPolicyFragmentClient _policyFragmentClient;
    private readonly EntitiesRegistry _registry;
    public PolicyFragmentsCopyHandler(IPolicyFragmentClient policyFragmentClient, EntitiesRegistry registry) : base(registry)
    {
        this._policyFragmentClient = policyFragmentClient;
        this._registry = registry;
    }
    public override EntityType UsedEntities => EntityType.PolicyFragment;

    public override Type OperationType => typeof(CopyOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        //PolicyFragmentsResource
        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);
        var policyFragmentTemplate = (PolicyFragmentsResource) copyOperation.Entity.ArmTemplate;
        var newTemplate = ModifyTemplate(workspaceId, policyFragmentTemplate);

        var entity = new Entity(newTemplate.Name,
            EntityType.PolicyFragment,
            newTemplate.Name,
            newTemplate);
        this._registry.RegisterMapping(operation.Entity, entity);

        return this._policyFragmentClient.Create(newTemplate, workspaceId);
    }

    static PolicyFragmentsResource ModifyTemplate(string workspaceId, PolicyFragmentsResource template)
    {
        var groupTemplate = template.Copy();
        groupTemplate.Name = $"{groupTemplate.Name}-in-{workspaceId}";
        return groupTemplate;
    }
}
