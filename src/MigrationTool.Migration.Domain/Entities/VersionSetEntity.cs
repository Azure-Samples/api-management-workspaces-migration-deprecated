using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.ApiVersionSet;

namespace MigrationTool.Migration.Domain.Entities;

public class VersionSetEntity : Entity, IEquatable<VersionSetEntity>
{
    public ApiVersionSetTemplateResource ArmTemplate { get; set; }

    public VersionSetEntity(string id) : base(id, EntityType.VersionSet)
    {
    }

    public VersionSetEntity(string id, string displayName, ApiVersionSetTemplateResource armTemplate) : this(id)
    {
        this.DisplayName = displayName;
        this.ArmTemplate = armTemplate;
    }

    public List<ApiEntity> Apis { get; set; } = new List<ApiEntity>();

    public override int GetHashCode() => base.GetHashCode();

    public virtual bool Equals(VersionSetEntity? other) => base.Equals(other);
}