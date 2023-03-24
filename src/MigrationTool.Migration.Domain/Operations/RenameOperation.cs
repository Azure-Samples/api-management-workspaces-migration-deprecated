using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Operations;

public record RenameOperation(Entity Entity) : IMigrationOperation
{
    public override string ToString() => $"Rename: {this.Entity}";
}