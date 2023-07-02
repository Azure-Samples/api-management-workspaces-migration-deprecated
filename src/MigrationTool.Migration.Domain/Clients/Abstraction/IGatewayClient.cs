
using MigrationTool.Migration.Domain.Entities;

namespace MigrationTool.Migration.Domain.Clients.Abstraction;

public interface IGatewayClient
{
    public Task<bool> IsLinkedWithGateway(ApiEntity api);
}
