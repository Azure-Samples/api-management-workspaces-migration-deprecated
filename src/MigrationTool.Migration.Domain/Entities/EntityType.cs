using System.ComponentModel;
using System.Configuration;

namespace MigrationTool.Migration.Domain.Entities;

[Flags]
public enum EntityType
{
    [Description("API")]
    Api = 1 << 0,
    [Description("API Operation")]
    ApiOperation = 1 << 1,
    Product = 1 << 2,
    Subscription = 1 << 3,
    Group = 1 << 4,
    [Description("Policy Fragment")]
    PolicyFragment = 1 << 5,
    [Description("Named Value")]
    NamedValue = 1 << 6,
    Tag = 1 << 7,
    [Description("Version Set")]
    VersionSet = 1 << 8,
    [Description("User")]
    User = 1 << 9,
    [Description("Schema")]
    Schema = 1 << 10,
}