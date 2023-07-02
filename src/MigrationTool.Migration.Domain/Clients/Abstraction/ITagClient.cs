using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Tags;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients.Abstraction;

public interface ITagClient
{
    public Task<IReadOnlyCollection<Entity>> FetchEntities(string tagId);

    public Task Create(TagTemplateResource resource, string workspaceId);

    public Task ConnectWithProduct(Entity tag, Entity product, string workspaceId);

    public Task ConnectWithApi(Entity tag, Entity api, string workspaceId);

    public Task ConnectWithApiOperation(Entity tag, OperationEntity apiOperation, string workspaceId);
}
