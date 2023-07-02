using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiVersionSet;
using MigrationTool.Migration.Domain.Entities;
using System.Net.Http.Json;

namespace MigrationTool.Migration.Domain.Clients.Abstraction;

public interface IVersionSetClient
{
    public Task<Entity?> FetchVersionSet(Entity api);

    public Task<Entity> Create(ApiVersionSetTemplateResource versionSetTemplate, string workspace);

    public Task<Entity> Update(ApiVersionSetTemplateResource versionSetTemplate, string workspace);

    public Task Delete(Entity api);
}
