using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;

namespace MigrationTool.Migration.Domain.Entities;

public record Entity(string Id, string DisplayName, EntityType Type, TemplateResource ArmTemplate)
{
    public override string ToString() 
        => $"{this.Type}: {this.DisplayName}";

    public override int GetHashCode() 
        => HashCode.Combine(this.Id, (int)this.Type);

    public virtual bool Equals(Entity? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return this.Id == other.Id && this.Type == other.Type;
    }
};