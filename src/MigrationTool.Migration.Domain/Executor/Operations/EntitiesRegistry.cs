using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class EntitiesRegistry
{
    private readonly Dictionary<Entity, Entity> entities = new();

    public void RegisterMapping(Entity originalEntity, Entity newEntity) =>
        this.entities.Add(originalEntity, newEntity);

    public bool TryGetMapping(Entity originalEntity, out Entity newEntity) =>
        this.entities.TryGetValue(originalEntity, out newEntity);
}