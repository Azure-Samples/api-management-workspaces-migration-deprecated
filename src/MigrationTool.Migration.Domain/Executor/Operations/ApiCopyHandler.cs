using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiOperations;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Extensions;
using MigrationTool.Migration.Domain.Operations;
using Newtonsoft.Json.Linq;

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

        var newApi = await this.CopyApi(workspaceId, originalEntity);
        this.registry.RegisterMapping(originalEntity, newApi);

        foreach (var revision in originalEntity.Revisions)
        {
            await this.CopyApi(workspaceId, revision);
        }
    }

    async Task<Entity> CopyApi(string workspaceId, ApiEntity originalEntity)
    {
        var template = ModifyTemplate(workspaceId, originalEntity.ArmTemplate);

        var openApiDef = await this.apiClient.ExportOpenApiDefinition(originalEntity.Id);

        var json = JObject.Parse(openApiDef);
        var requestPayload = new JObject();
        requestPayload["properties"] = json;
        requestPayload["properties"]["path"] = template.Properties.Path;
        requestPayload["properties"]["value"]["info"]["title"] = template.Properties.DisplayName;

        var newApi = await this.apiClient.Create(template, workspaceId);

        await this.apiClient.ImportOpenApiDefinition(requestPayload.ToString(), newApi.Id, workspaceId);

        var apiPolicy = await this.apiClient.FetchPolicy(newApi.Id);
        if (apiPolicy != null)
        {
            var modifiedPolicy = this.policyModifier.Modify(apiPolicy);
            await this.apiClient.UploadApiPolicy(newApi, modifiedPolicy, workspaceId);
        }

        foreach (var originalOperation in await this.apiClient.FetchOperations(originalEntity.Id))
        {
            var policy = await this.apiClient.FetchOperationPolicy(originalEntity.Id, originalOperation.Id);
            if (policy != null)
            {
                var modifiedPolicy = this.policyModifier.Modify(policy);
                await this.apiClient.UploadApiOperationPolicy(newApi.Id, originalOperation.Id, modifiedPolicy, workspaceId);
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