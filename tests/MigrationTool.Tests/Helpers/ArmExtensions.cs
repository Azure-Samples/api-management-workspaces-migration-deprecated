
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using Newtonsoft.Json;

namespace MigrationTool.Tests.Helpers;

public static class ArmExtensions
{
    public static bool TestEquality(this TemplateResource obj, TemplateResource another)
    {
        return JsonConvert.SerializeObject(obj).Equals(Newtonsoft.Json.JsonConvert.SerializeObject(another));
    }
}
