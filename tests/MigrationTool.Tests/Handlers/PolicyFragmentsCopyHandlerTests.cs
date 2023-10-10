using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.PolicyFragments;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Migration.Domain.Operations;
using Moq;
using System.Threading.Tasks;

namespace MigrationTool.Tests.Handlers;

[TestClass]
public class PolicyFragmentsCopyHandlerTests : BaseTest
{
    private PolicyFragmentsCopyHandler handler;

    [TestInitialize]
    public void Initialize()
    {
        this.handler = new PolicyFragmentsCopyHandler(policyFragmentClient.Object, entitiesRegistry);
    }

    [TestMethod]
    public async Task Handle()
    {
        //arrange
        var workspaceId = "workspace-id";
        var policyFragment = new Entity("some-id", EntityType.PolicyFragment, "name", new PolicyFragmentsResource() { Name = "some-id", Properties = new PolicyFragmentsProperties() { Description = "description1", Format = "format1", Value = "value1" } });

        //act
        await this.handler.Handle(new CopyOperation(policyFragment), workspaceId);
        var gotMapping = this.entitiesRegistry.TryGetMapping(policyFragment, out var newPolicyFragment);

        //verify
        policyFragmentClient.Verify(c => c.Create(It.IsAny<PolicyFragmentsResource>(), workspaceId));
        Assert.IsTrue(gotMapping);
        Assert.AreEqual(newPolicyFragment.ArmTemplate.Name, policyFragment.ArmTemplate.Name + "-in-" + workspaceId);
        Assert.AreEqual(((PolicyFragmentsResource)newPolicyFragment.ArmTemplate).Name, ((PolicyFragmentsResource)policyFragment.ArmTemplate).Name + "-in-" + workspaceId);
    }
}
