using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Schemas;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients.Abstraction;

public interface ISchemasClient
{
    public Task<IReadOnlyCollection<Entity>> Fetch(IReadOnlyCollection<string> schemasNames);

    public Task<Entity> Create(SchemaTemplateResource resource, string workspaceId);
}
