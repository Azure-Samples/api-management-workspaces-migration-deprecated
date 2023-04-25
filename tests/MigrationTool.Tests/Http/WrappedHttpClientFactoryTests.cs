// --------------------------------------------------------------------------
//  <copyright file="BasicTest.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//  </copyright>
// --------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using MigrationTool.Http;
using Moq;

namespace MigrationTool.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WrappedHttpClientFactoryTests
    {
        [TestMethod]
        public void CreateClientWithUserAgent()
        {
            // Arrange
            var applicationName = nameof(WrappedHttpClientFactoryTests);
            var buildVersion = Guid.NewGuid().ToString();
            var applicationInfoMock = new Mock<IApplicationInfo>();
            applicationInfoMock.SetupGet(x => x.Name).Returns(applicationName);
            applicationInfoMock.SetupGet(x => x.BuildVersion).Returns(buildVersion);
            var httpClientBuilderMock = new Mock<IHttpClientFactory>();
            httpClientBuilderMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            var wrappedHttpClientFactory = new WrappedHttpClientFactory(applicationInfoMock.Object, httpClientBuilderMock.Object);
            
            // Act
            var createdClient = wrappedHttpClientFactory.CreateClient();
            
            // Assert
            Assert.IsNotNull(createdClient);
            Assert.IsNotNull(createdClient.DefaultRequestHeaders);
            Assert.IsNotNull(createdClient.DefaultRequestHeaders.UserAgent);
            Assert.IsTrue(createdClient.DefaultRequestHeaders.UserAgent.ToString().Contains($"{applicationName}/{buildVersion}"));
        }
    }
}