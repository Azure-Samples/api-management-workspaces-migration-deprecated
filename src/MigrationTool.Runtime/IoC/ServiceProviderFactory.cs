using Microsoft.Azure.Management.ApiManagement.ArmTemplates;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Commands.Configurations;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Extensions.DependencyInjection;
using MigrationTool.Http;
using MigrationTool.Migration.Domain;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Dependencies;
using MigrationTool.Migration.Domain.Dependencies.Resolvers;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Migration.Domain.Operations;

namespace MigrationTool.IoC
{
    public class ServiceProviderFactory
    {
        public static IServiceProvider CreateServiceProvider(MigrationProgramConfig config, ApplicationInfo applicationInfo)
        {
            var rootDependencies = new ServiceCollection()
                .AddHttpClient()
                .BuildServiceProvider();
        
            IServiceCollection collection = new ServiceCollection();
            ServiceExtensions.AddArmTemplatesServices(collection, null);

            var extractorParameters = new ExtractorParameters(new ExtractorConsoleAppConfiguration
            {
                ResourceGroup = config.ResourceGroup,
                SourceApimName = config.ServiceName
            });
            collection.AddSingleton(extractorParameters);

            // We rely on our devops toolkit which creates clients, but since we cannot change how it handles them
            // we are wrapping the HTTP client factory to be able to influence how the client is being created
            collection.AddSingleton<IHttpClientFactory, WrappedHttpClientFactory>(s => new WrappedHttpClientFactory(applicationInfo, rootDependencies.GetRequiredService<IHttpClientFactory>()));
            collection.AddSingleton<IApplicationInfo, ApplicationInfo>(_ => applicationInfo);

            collection.AddSingleton<ApiClient, ApiClient>();
            collection.AddSingleton<NamedValuesClient, NamedValuesClient>();
            collection.AddSingleton<PolicyFragmentsClient, PolicyFragmentsClient>();
            collection.AddSingleton<ProductClient, ProductClient>();
            collection.AddSingleton<WorkspaceClient, WorkspaceClient>();
            collection.AddSingleton<SubscriptionClient, SubscriptionClient>();

            collection.AddSingleton<PolicyRelatedDependenciesResolver, PolicyRelatedDependenciesResolver>();
            collection.AddSingleton<DependencyService, DependencyService>();
            collection.AddSingleton<IEntityDependencyResolver, ApiDependencyResolver>();
            collection.AddSingleton<IEntityDependencyResolver, ProductDependencyResolver>();
            collection.AddSingleton<IEntityDependencyResolver, NamedValueDependencyResolver>();
            collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.Group));
            collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.ApiOperation));
            collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.Tag));
            collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.PolicyFragment));
            collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.Subscription));

            collection.AddSingleton<DependencyGraphBuilder, DependencyGraphBuilder>();
            collection.AddSingleton<EntitiesRegistry, EntitiesRegistry>();
            collection.AddSingleton<PolicyModifier, PolicyModifier>();
            collection.AddSingleton<MigrationPlanExecutor, MigrationPlanExecutor>();
            collection.AddSingleton<OperationHandler, ApiCopyOperationHandler>();
            collection.AddSingleton<OperationHandler, ProductCopyOperationHandler>();
            collection.AddSingleton<OperationHandler, ProductApiConnectionHandler>();
            collection.AddSingleton<OperationHandler, SubscriptionCopyHandler>();
            collection.AddSingleton<OperationHandler>(_ =>
                new EmptyHandler(EntityType.Api | EntityType.Subscription, typeof(ConnectOperation)));
            collection.AddSingleton<OperationHandler>(_ =>
                new EmptyHandler(EntityType.Product | EntityType.Subscription, typeof(ConnectOperation)));

            collection.AddSingleton<OperationHandler, NamedValueCopyHandler>();
            collection.AddSingleton<OperationHandler>(_ =>
                new EmptyHandler(EntityType.Api | EntityType.NamedValue, typeof(ConnectOperation)));
            collection.AddSingleton<OperationHandler>(_ =>
                new EmptyHandler(EntityType.Product | EntityType.NamedValue, typeof(ConnectOperation)));

            return collection.BuildServiceProvider();
        }
    }
}