using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.TemplateModels;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients.Abstraction;

public interface ISubscriptionClient
{
    public Task<IReadOnlyCollection<Entity>> FetchForApi(string entityId);

    public Task<IReadOnlyCollection<Entity>> FetchForProduct(string entityId);

    public Task<Entity> Create(SubscriptionsTemplateResource resource, string workspaceId);
}
