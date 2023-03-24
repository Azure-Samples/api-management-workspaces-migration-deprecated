using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients;

public class PolicyFragmentsClient
{
    public Task<IReadOnlyCollection<Entity>> Fetch(IReadOnlyCollection<string> policyFragmentsNames)
    {
        Console.Out.WriteLine($"Policy fragments to fetch {string.Join(", ", policyFragmentsNames)}");
        // TODO: Implement
        return Task.FromResult<IReadOnlyCollection<Entity>>(new List<Entity>());
    }
}