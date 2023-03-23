using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients;

public class NamedValuesClient
{
    
    public Task<Entity> Fetch(string id)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Entity>> Fetch(IReadOnlyCollection<string> namedValuesNames)
    {
        Console.Out.WriteLine($"Named values to fetch {string.Join(", ", namedValuesNames)}");
        // TODO: Implement
        return Task.FromResult<IReadOnlyCollection<Entity>>(new List<Entity>());
    }
}