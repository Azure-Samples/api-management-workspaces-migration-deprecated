namespace MigrationTool.Migration.Domain.Entities;

[Flags]
public enum EntityType
{
    Api = 1 << 0,
    ApiOperation = 1 << 1,
    Product = 1 << 2,
    Subscription = 1 << 3,
    Group = 1 << 4,
    PolicyFragment = 1 << 5,
    NamedValue = 1 << 6,
    Tag = 1 << 7,
}