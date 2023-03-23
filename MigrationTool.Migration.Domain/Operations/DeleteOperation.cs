using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Operations;

public record DeleteOperation(Entity Entity) : IMigrationOperation
{
    public override string ToString() => $"Delete: {this.Entity}";
}