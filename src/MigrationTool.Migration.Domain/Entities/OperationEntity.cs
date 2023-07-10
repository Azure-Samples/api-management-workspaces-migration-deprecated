using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationTool.Migration.Domain.Entities;

public class OperationEntity : Entity, IEquatable<OperationEntity>
{
    public string ApiId { get; }
    public OperationEntity(string id, string apiId)
    : base(id, EntityType.ApiOperation)
    {
        ApiId = apiId;
    }

    public override int GetHashCode() => base.GetHashCode();

    public virtual bool Equals(OperationEntity? other) => base.Equals(other);
}
