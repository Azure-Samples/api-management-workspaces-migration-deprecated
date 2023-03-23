using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Planner;

public class MigrationPlan
{
    private readonly List<IMigrationOperation> operations = new();
    
    public IReadOnlyCollection<IMigrationOperation> Operations => this.operations;

    internal void AddCopyOperation(Entity entity) =>
        this.operations.Add(new CopyOperation(entity));

    internal void AddRenameOperation(Entity entity) =>
        this.operations.Add(new RenameOperation(entity));

    internal void AddDeleteOperation(Entity entity) =>
        this.operations.Add(new DeleteOperation(entity));

    internal void AddConnectOperation(Entity entityLeft, Entity entityRight) =>
        this.operations.Add(new ConnectOperation(entityLeft, entityRight));

    public override string ToString()
    {
        return $"Migration plan: \n{string.Join("\n", this.operations)}";
    }
}
