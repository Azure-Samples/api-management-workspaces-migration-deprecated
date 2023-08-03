using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Groups;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients.Abstraction;

public interface IGroupsClient
{
    public Task<IReadOnlyCollection<Entity>> FetchProducts(string groupId);

    public Task<IReadOnlyCollection<Entity>> FetchUsers(string groupId);

    public Task ConnectWithUser(Entity group, Entity user, string workspaceId);

    public Task<Entity> Create(GroupTemplateResource resource, string workspaceId);

    public Task ConnectWithProduct(Entity group, Entity product, string workspaceId);

}
