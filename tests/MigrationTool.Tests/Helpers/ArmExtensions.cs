
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using System;

namespace MigrationTool.Tests.Helpers;

public static class ArmExtensions
{
    public static bool TestEquality(this TemplateResource obj, TemplateResource another)
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(obj).Equals(Newtonsoft.Json.JsonConvert.SerializeObject(another));
    }
}
