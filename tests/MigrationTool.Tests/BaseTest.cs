using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Commands.Configurations;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.API.Clients.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Tests.Helpers;
using Moq;
using Moq.Contrib.HttpClient;
using Moq.Protected;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MigrationTool.Tests;

public class BaseTest
{
    protected ExtractorParameters extractorParameters = new ExtractorParameters(new ExtractorConsoleAppConfiguration() { ResourceGroup = "test-rg", SourceApimName = "test-apim" });
    protected Mock<IApisClient> apisClient = new Mock<IApisClient>();
    protected Mock<IProductsClient> productsClient = new Mock<IProductsClient>();
    protected Mock<IApiOperationClient> apiOperationClient = new Mock<IApiOperationClient>();
    protected Mock<IPolicyClient> policyClient = new Mock<IPolicyClient>();
    protected Mock<HttpMessageHandler> httpHandler = new Mock<HttpMessageHandler>();
    protected EntitiesRegistry entitiesRegistry = new EntitiesRegistry();
    protected Mock<AzureCliAuthenticator> auth = new Mock<AzureCliAuthenticator>();

    protected string subscription = "test-subscription";

    public BaseTest()
    {
        this.auth.Setup(auth => auth.GetAccessToken().Result).Returns(("someToken", this.subscription));
    }

    protected IComparer comparer = new ArmTemplateComparer();
}
