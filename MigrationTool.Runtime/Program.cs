﻿using CommandLine;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Commands.Configurations;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extractor.Models;
using Microsoft.Extensions.DependencyInjection;
using MigrationTool.Migration.Domain;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Dependencies;
using MigrationTool.Migration.Domain.Dependencies.Resolvers;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor;
using MigrationTool.Migration.Domain.Executor.Operations;
using MigrationTool.Migration.Domain.Planner;
using Sharprompt;
using System;

public class Program
{
    public static IServiceProvider ServiceProvider;


    public static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<MigrationProgramConfig>(args).WithParsedAsync(MigrationProgram);
    }


    public static async Task MigrationProgram(MigrationProgramConfig config)
    {
        ServiceProvider = CreateServiceProvider(config);
        Console.WriteLine("Fetching apis...");
        var apis = await ChooseApis();
        Console.WriteLine("Fetching workspaces...");
        var workspace = await ChooseWorkspace();
        if (workspace == null)
        {
            Console.WriteLine($"No workspaces");
            return;
        }
        var dependencyGraphBuilder = ServiceProvider.GetRequiredService<DependencyGraphBuilder>();
        Console.WriteLine("Fetching dependencies...");
        var graph = await dependencyGraphBuilder.Build(apis);
        Console.WriteLine("Building migration plan...");
        var plan = MigrationPlanner.Plan(graph, MigrationType.Copy);
        Console.WriteLine(plan);
        if (Prompt.Confirm("Confirm migration plan?"))
        {
            var executor = ServiceProvider.GetRequiredService<MigrationPlanExecutor>();
            Console.WriteLine($"Migrating...");
            await executor.Execute(plan, workspace);
            Console.WriteLine($"Migration successful");
        }
    }

    private static async Task<IEnumerable<Entity>> ChooseApis()
    {
        var apisClient = ServiceProvider.GetRequiredService<ApiClient>();
        var apis = await apisClient.FetchAllApis();
        return Prompt.MultiSelect("Select apis to migrate", apis);
    }
    
    private static async Task<string?> ChooseWorkspace()
    {
        var workspaceService = ServiceProvider.GetRequiredService<WorkspaceClient>();
        var workspaces = await workspaceService.FetchAll();
        if (workspaces.Count > 0)
            return Prompt.Select("To which workspace you want to migrate?", workspaces);
        return null;
    }

    private static IServiceProvider CreateServiceProvider(MigrationProgramConfig config)
    {
        IServiceCollection collection = new ServiceCollection();
        ServiceExtensions.AddArmTemplatesServices(collection, null);

        var extractorParamters = new ExtractorParameters(new ExtractorConsoleAppConfiguration()
            { ResourceGroup = config.ResourceGroup, SourceApimName = config.ServiceName });
        collection.AddSingleton(extractorParamters);


        collection.AddSingleton<ApiClient, ApiClient>();
        collection.AddSingleton<NamedValuesClient, NamedValuesClient>();
        collection.AddSingleton<PolicyFragmentsClient, PolicyFragmentsClient>();
        collection.AddSingleton<ProductClient, ProductClient>();
        collection.AddSingleton<WorkspaceClient, WorkspaceClient>();

        collection.AddSingleton<PolicyRelatedDependenciesResolver, PolicyRelatedDependenciesResolver>();
        collection.AddSingleton<DependencyService, DependencyService>();
        collection.AddSingleton<IEntityDependencyResolver, ApiDependencyResolver>();
        collection.AddSingleton<IEntityDependencyResolver, ProductDependencyResolver>();
        collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.Group));
        collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.ApiOperation));
        collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.Tag));
        collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.PolicyFragment));
        collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.NamedValue));
        collection.AddSingleton<IEntityDependencyResolver>(_ => new NoDependencyResolver(EntityType.Subscription));
        collection.AddSingleton<DependencyGraphBuilder, DependencyGraphBuilder>();
        collection.AddSingleton<EntitiesRegistry, EntitiesRegistry>();
        collection.AddSingleton<MigrationPlanExecutor, MigrationPlanExecutor>();
        collection.AddSingleton<IOperationHandler, ApiCopyOperationHandler>();
        collection.AddSingleton<IOperationHandler, ProductCopyOperationHandler>();
        collection.AddSingleton<IOperationHandler, ProductApiConnectionHandler>();
        
        return collection.BuildServiceProvider();
    }
}