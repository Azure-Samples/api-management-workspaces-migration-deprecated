
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrationTool.Tests.Helpers;

public static class ArmExtensions
{
    public static bool TestEquality(this TemplateResource obj, TemplateResource another)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new OrderedContractResolver()
        };
        return JsonConvert.SerializeObject(obj, settings).Equals(JsonConvert.SerializeObject(another, settings));
    }
}

internal class OrderedContractResolver: DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var @base = base.CreateProperties(type, memberSerialization);
        var ordered = @base
            .OrderBy(p => p.Order ?? int.MaxValue)
            .ThenBy(p => p.PropertyName)
            .ToList();
        return ordered;
    }
}
