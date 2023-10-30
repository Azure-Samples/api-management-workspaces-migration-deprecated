using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.PolicyFragments;
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients.Abstraction;

public interface IPolicyFragmentClient
{
    public Task<IReadOnlyCollection<Entity>> Fetch(IReadOnlyCollection<string> policyFragmentsNames);

    public Task<Entity> Create(PolicyFragmentsResource resource, string workspaceId);
}
