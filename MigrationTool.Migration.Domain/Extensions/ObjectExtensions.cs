using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Extensions;

namespace MigrationTool.Migration.Domain.Extensions;

public static class ObjectExtensions
{
    public static T Copy<T>(this T source) => source.Serialize().Deserialize<T>();
}