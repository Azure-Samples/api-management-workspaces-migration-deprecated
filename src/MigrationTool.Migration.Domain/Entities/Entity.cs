using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using MigrationTool.Migration.Domain.Extensions;
using System.Configuration;

namespace MigrationTool.Migration.Domain.Entities;

public class Entity : IEquatable<Entity>
{
    public string Id { get; }
    public EntityType Type { get; }

    public string DisplayName { get; set; }
    public TemplateResource ArmTemplate { get; set; }

    public Entity(string id, EntityType type)
    {
        this.Id = id;
        this.Type = type;
    }

    public Entity(string id, EntityType type, string displayName, TemplateResource armTemplate)
        : this(id, type)
    {
        this.DisplayName = displayName;
        this.ArmTemplate = armTemplate;
    }

    public override string ToString()
    {
        var toString = ConfigurationManager.AppSettings["entityToString"];
        toString = toString.Replace("{type}", this.Type.GetDescription());
        toString = toString.Replace("{displayName}", this.DisplayName);
        toString = toString.Replace("{id}", this.Id);
        return toString;
    }

    public override int GetHashCode()
        => HashCode.Combine(this.Id, (int)this.Type);

    public virtual bool Equals(Entity? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return this.Id == other.Id && this.Type == other.Type;
    }
}