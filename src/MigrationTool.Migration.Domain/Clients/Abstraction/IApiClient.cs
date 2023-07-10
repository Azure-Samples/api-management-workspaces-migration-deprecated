using MigrationTool.Migration.Domain.Entities;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;

namespace MigrationTool.Migration.Domain.Clients.Abstraction;

public interface IApiClient
{
    public Task<IReadOnlyCollection<Entity>> FetchAllApisAndVersionSets();

    public Task<IReadOnlyCollection<Entity>> FetchAllApisFlat();

    public Task<IReadOnlyCollection<Entity>> FetchProducts(string entityId);

    public Task<Entity> Fetch(string apiId);

    public Task<string?> FetchPolicy(string entityId);

    public Task<IReadOnlyCollection<Entity>> FetchTags(string entityId);

    public Task<IReadOnlyCollection<Entity>> FetchOperations(string entityId);

    public Task<string?> FetchOperationPolicy(string entityId, string operationId);

    public Task<IReadOnlyCollection<Entity>> FetchOperationTags(string entityId, string operationId);

    public Task<string> ExportOpenApiDefinition(string apiId);

    public Task<Entity> ImportOpenApiDefinition(string apiDefinition, string apiId, string workspace);

    public Task<Entity> Create(ApiTemplateResource resource, string workspace);

    public Task UploadApiPolicy(Entity api, string policy, string workspace);

    public Task UploadApiOperationPolicy(string apiId, string operationId, string policy, string workspace);

    public Task Delete(Entity api);
}
