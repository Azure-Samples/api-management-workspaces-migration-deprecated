using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Operations;

public record ConnectOperation(Entity Entity, Entity ConnectToEntity) : IMigrationOperation
{
    public EntityType EntityType => this.Entity.Type | this.ConnectToEntity.Type;
    public override string ToString() => $"Connect: {this.Entity} -> {this.ConnectToEntity}";
}

