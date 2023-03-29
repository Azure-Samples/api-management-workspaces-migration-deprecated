using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Apis;

namespace MigrationTool.Migration.Domain.Entities;

public class ApiEntity : Entity, IEquatable<ApiEntity>
{
    public ApiTemplateResource ArmTemplate => (ApiTemplateResource)base.ArmTemplate;

    public ApiEntity(string id)
        : base(id, EntityType.Api)
    {
    }

    public ApiEntity(string id, string displayName, ApiTemplateResource armTemplate)
        : base(id, EntityType.Api, displayName, armTemplate)
    {
    }

    public List<ApiEntity> Revisions { get; set; } = new List<ApiEntity>();

    public override int GetHashCode() => base.GetHashCode();

    public virtual bool Equals(ApiEntity? other) => base.Equals(other);
}