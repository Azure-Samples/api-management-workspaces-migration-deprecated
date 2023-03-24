using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Operations;

public interface IMigrationOperation
{
    public Entity Entity { get; }

    public EntityType EntityType => this.Entity.Type;

    public string ToString();
}