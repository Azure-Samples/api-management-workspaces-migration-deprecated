using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Commands.Configurations;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Dependencies.Resolvers;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Tests.Helpers;
using Moq;
using System.Collections;
using System.Net.Http;
using ARM = Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;

namespace MigrationTool.Tests;

public class BaseTest
{
    protected ExtractorParameters extractorParameters = new ExtractorParameters(new ExtractorConsoleAppConfiguration() { ResourceGroup = "test-rg", SourceApimName = "test-apim" });
    protected Mock<ARM.IApisClient> armApisClient = new Mock<ARM.IApisClient>();
    protected Mock<ARM.IProductsClient> armProductsClient = new Mock<ARM.IProductsClient>();
    protected Mock<ARM.IApiOperationClient> armApiOperationClient = new Mock<ARM.IApiOperationClient>();
    protected Mock<ARM.IPolicyClient> armPolicyClient = new Mock<ARM.IPolicyClient>();
    protected Mock<HttpMessageHandler> httpHandler = new Mock<HttpMessageHandler>();
    protected EntitiesRegistry entitiesRegistry = new EntitiesRegistry();
    protected Mock<AzureCliAuthenticator> auth = new Mock<AzureCliAuthenticator>();
    protected Mock<ARM.ITagClient> armTagClient = new Mock<ARM.ITagClient>();
    protected Mock<ARM.IApiRevisionClient> armApiRevisionClient = new Mock<ARM.IApiRevisionClient>();
    protected IApiDataProcessor armApiDataProcessor = new ApiDataProcessor();
    protected Mock<IApiClient> apiClient = new Mock<IApiClient>();
    protected Mock<IProductClient> productClient = new Mock<IProductClient>();
    protected Mock<ISubscriptionClient> subscriptionClient = new Mock<ISubscriptionClient>();
    protected Mock<IGatewayClient> gatewayClient = new Mock<IGatewayClient>();
    protected Mock<IPolicyRelatedDependenciesResolver> policyRelatedDependencyResolver = new Mock<IPolicyRelatedDependenciesResolver>();
    protected Mock<IVersionSetClient> versionSetClient = new Mock<IVersionSetClient>();
    protected Mock<ITagsDependencyResolver> tagsDependencyResolver = new Mock<ITagsDependencyResolver>();
    protected Mock<ITagClient> tagClient = new Mock<ITagClient>();

    protected IComparer comparer = new ArmTemplateComparer();

    protected static string subscription = "test-subscription";

    public BaseTest()
    {
        auth.Setup(auth => auth.GetAccessToken().Result).Returns(("someToken", subscription));
    }
}
