using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Commands.Configurations;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.ApiOperations;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.ApiRevision;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Apis;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Policy;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Product;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Tags;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities.DataProcessors.Absctraction;
using MigrationTool.Migration.Domain.Clients.Abstraction;
using MigrationTool.Migration.Domain.Dependencies.Resolvers;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Tests.Helpers;
using Moq;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MigrationTool.Tests;

public class BaseTest
{
    protected ExtractorParameters extractorParameters = new ExtractorParameters(new ExtractorConsoleAppConfiguration() { ResourceGroup = "test-rg", SourceApimName = "test-apim" });
    protected Mock<IApisClient> armApisClient = new Mock<IApisClient>();
    protected Mock<IProductsClient> armProductsClient = new Mock<IProductsClient>();
    protected Mock<IApiOperationClient> armApiOperationClient = new Mock<IApiOperationClient>();
    protected Mock<IPolicyClient> armPolicyClient = new Mock<IPolicyClient>();
    protected Mock<HttpMessageHandler> httpHandler = new Mock<HttpMessageHandler>();
    protected EntitiesRegistry entitiesRegistry = new EntitiesRegistry();
    protected Mock<AzureCliAuthenticator> auth = new Mock<AzureCliAuthenticator>();
    protected Mock<Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions.ITagClient> armTagClient = new Mock<Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions.ITagClient>();
    protected Mock<IApiRevisionClient> armApiRevisionClient = new Mock<IApiRevisionClient>();
    protected IApiDataProcessor armApiDataProcessor = new ApiDataProcessor();
    protected Mock<IApiClient> apiClient = new Mock<IApiClient>();
    protected Mock<IProductClient> productClient = new Mock<IProductClient>();
    protected Mock<ISubscriptionClient> subscriptionClient = new Mock<ISubscriptionClient>();
    protected Mock<Migration.Domain.Clients.Abstraction.IGatewayClient> gatewayClient = new Mock<Migration.Domain.Clients.Abstraction.IGatewayClient>();
    protected Mock<IPolicyRelatedDependenciesResolver> policyRelatedDependencyResolver = new Mock<IPolicyRelatedDependenciesResolver>();
    protected Mock<IVersionSetClient> versionSetClient = new Mock<IVersionSetClient>();
    protected Mock<ITagsDependencyResolver> tagsDependencyResolver = new Mock<ITagsDependencyResolver>();
    protected Mock<Migration.Domain.Clients.Abstraction.ITagClient> tagClient = new Mock<Migration.Domain.Clients.Abstraction.ITagClient>();

    protected IComparer comparer = new ArmTemplateComparer();

    protected static string subscription = "test-subscription";

    public BaseTest()
    {
        auth.Setup(auth => auth.GetAccessToken().Result).Returns(("someToken", subscription));
    }
}
