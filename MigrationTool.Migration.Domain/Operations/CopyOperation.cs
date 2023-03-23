using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Operations;

public record CopyOperation(Entity Entity) : IMigrationOperation
{
    public override string ToString() => $"Copy: {this.Entity}";
}