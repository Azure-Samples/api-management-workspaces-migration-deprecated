using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ProductApis;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients.Abstraction;

public interface IProductClient
{
    public Task<IReadOnlyCollection<Entity>> FetchApis(string entityId);

    public Task<string?> FetchPolicy(string entityId);

    public Task<Entity> CreateProduct(ProductApiTemplateResource resource, string workspace);

    public Task<Entity> UpdateProduct(ProductApiTemplateResource resource, string workspace);

    public Task<IReadOnlyCollection<Entity>> FetchTags(string entityId);

    public Task<IReadOnlyCollection<Entity>> FetchGroups(string entityId);

    public Task UploadProductPolicy(Entity product, string policy, string workspace);

    public Task DeleteProduct(Entity product);

    public Task AddApi(Entity product, Entity api, string productWorkspace, string apiWorkspace);

    public Task<Entity> Fetch(string id);
}
