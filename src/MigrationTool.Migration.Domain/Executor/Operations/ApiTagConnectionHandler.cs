using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class ApiTagConnectionHandler : OperationHandler
{
    private ITagClient tagClient;
    public ApiTagConnectionHandler(ITagClient tagClient, EntitiesRegistry registry) : base(registry)
    {
        this.tagClient = tagClient;
    }
    public override EntityType UsedEntities => EntityType.Tag | EntityType.Api;

    public override Type OperationType => typeof(ConnectOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var connectOperation = this.GetOperationOrThrow<ConnectOperation>(operation);

        Entity api;
        Entity tag;

        if (!this.tryGetNewEntity(connectOperation, EntityType.Tag, out tag))
        {
            throw new Exception($"Tag {tag.Id} not found");
        }
        if (!this.tryGetNewEntity(connectOperation, EntityType.Api, out api))
        {
            throw new Exception($"Api {api.Id} not found");
        }
        return this.tagClient.ConnectWithApi(tag, api, workspaceId);
    }
}
