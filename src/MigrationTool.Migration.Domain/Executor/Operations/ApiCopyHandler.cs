using System.Text.RegularExpressions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiOperations;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class ApiCopyOperationHandler : OperationHandler
{
    private static readonly Regex ApiIdWithRevision = new Regex("^(.*);rev=(.*)$");

    private readonly ApiClient apiClient;
    private readonly EntitiesRegistry registry;
    private readonly PolicyModifier policyModifier;

    public ApiCopyOperationHandler(ApiClient apiClient, EntitiesRegistry registry, PolicyModifier policyModifier)
    {
        this.apiClient = apiClient;
        this.registry = registry;
        this.policyModifier = policyModifier;
    }

    public override EntityType UsedEntities => EntityType.Api;
    public override Type OperationType => typeof(CopyOperation);

    public override async Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var copyOperation = this.GetOperationOrThrow<CopyOperation>(operation);
        var originalEntity = copyOperation.Entity as ApiEntity ?? throw new InvalidOperationException();

        var newTemplate = this.ModifyTemplate(workspaceId, originalEntity.ArmTemplate);

        var newApi = await this.CopyApi(workspaceId, newTemplate, originalEntity);
        this.registry.RegisterMapping(originalEntity, newApi);

        foreach (var revision in originalEntity.Revisions)
        {
            var revisionTemplate = this.ModifyTemplate(workspaceId, revision.ArmTemplate);
            await this.CopyApi(workspaceId, revisionTemplate, revision);
        }
    }

    async Task<Entity> CopyApi(string workspaceId, ApiTemplateResource newTemplate, ApiEntity originalEntity)
    {
        var newApi = await this.apiClient.Create(newTemplate, workspaceId);

        var apiPolicy = await this.apiClient.FetchPolicy(newApi.Id);
        if (apiPolicy != null)
        {
            var modifiedPolicy = this.policyModifier.Modify(apiPolicy);
            await this.apiClient.UploadApiPolicy(newApi, modifiedPolicy, workspaceId);
        }

        foreach (var originalOperation in await this.apiClient.FetchOperations(originalEntity.Id))
        {
            var operationTemplate = (ApiOperationTemplateResource)originalOperation.ArmTemplate;
            var newOperation = await this.apiClient.CreateOperation(newApi.Id, operationTemplate, workspaceId);
            var policy = await this.apiClient.FetchOperationPolicy(originalEntity.Id, originalOperation.Id);
            if (policy != null)
            {
                var modifiedPolicy = this.policyModifier.Modify(policy);
                await this.apiClient.UploadApiOperationPolicy(newApi.Id, newOperation.Id, modifiedPolicy, workspaceId);
            }
        }

        return newApi;
    }

    private ApiTemplateResource ModifyTemplate(string workspaceId, ApiTemplateResource template)
    {
        var newTemplate = template.Copy();

        newTemplate.Name = ApiIdWithRevision.Replace(template.Name, $"$1-in-{workspaceId};rev=$2");
        newTemplate.Properties.DisplayName = $"{template.Properties.DisplayName}-in-{workspaceId}";
        newTemplate.Properties.Path = $"{template.Properties.Path}-in-{workspaceId}";

        if (template.Properties.ApiVersionSetId != null)
        {
            Entity originalVersionSet = new VersionSetEntity(template.Properties.ApiVersionSetId);
            if (!this.registry.TryGetMapping(originalVersionSet, out var newVersionSet))
                throw new Exception("Version set has not been found in the registry");
            newTemplate.Properties.ApiVersionSetId = newVersionSet.Id;
        }

        return newTemplate;
    }
}