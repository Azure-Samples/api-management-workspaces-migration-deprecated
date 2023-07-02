using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Dependencies.Resolvers;

public interface IPolicyRelatedDependenciesResolver
{
    public Task<IReadOnlyCollection<Entity>> Resolve(string policy);
}
