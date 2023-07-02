using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationTool.Migration.Domain.Executor.Operations;

public class ApiOperationTagConnectionHandler : OperationHandler
{
    private ITagClient tagClient;
    public ApiOperationTagConnectionHandler(ITagClient tagClient, EntitiesRegistry registry) : base(registry)
    {
        this.tagClient = tagClient;
    }
    public override EntityType UsedEntities => EntityType.Tag | EntityType.ApiOperation;

    public override Type OperationType => typeof(ConnectOperation);

    public override Task Handle(IMigrationOperation operation, string workspaceId)
    {
        var connectOperation = this.GetOperationOrThrow<ConnectOperation>(operation);

        Entity apiOperation;
        Entity tag;

        this.tryGetNewEntity(connectOperation, EntityType.Tag, out tag);
        if (!this.tryGetNewEntity(connectOperation, EntityType.ApiOperation, out apiOperation))
        {
            throw new Exception($"ApiOperation {apiOperation.Id} not found");
        }
        return this.tagClient.ConnectWithApiOperation(tag, (OperationEntity) apiOperation, workspaceId);
    }
}
