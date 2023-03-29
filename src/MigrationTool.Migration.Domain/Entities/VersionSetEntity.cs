using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;

namespace MigrationTool.Migration.Domain.Entities;
public record VersionSetEntity(string Id) : Entity(Id, EntityType.VersionSet)
{
    public VersionSetEntity(string id, string displayName, TemplateResource armTemplate): this(id)
    {
        this.DisplayName = displayName;
        this.ArmTemplate = armTemplate;
    }

    public List<Entity> Apis { get; set; } = new List<Entity>();

    public override string ToString()
        => $"Version Set: {this.DisplayName}";

    public override int GetHashCode()
    => HashCode.Combine(this.Id, (int)this.Type);

    public virtual bool Equals(VersionSetEntity? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return this.Id == other.Id && this.Type == other.Type;
    }
}
