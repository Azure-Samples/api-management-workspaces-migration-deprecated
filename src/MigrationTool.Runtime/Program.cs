using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using MigrationTool;
using MigrationTool.IoC;
using MigrationTool.Migration.Domain;
using MigrationTool.Migration.Domain.Clients;
using MigrationTool.Migration.Domain.Dependencies;
using MigrationTool.Migration.Domain.Entities;
using MigrationTool.Migration.Domain.Executor;
using MigrationTool.Migration.Domain.Planner;
using Sharprompt;

public class Program
{
    public static IServiceProvider ServiceProvider;

    public static async Task Main(string[] args)
    {
        var applicationInfo = ApplicationInfo.GetInfo();
        Console.WriteLine("Welcome to the Azure API Management Workspaces migration tool! (v{0})",  applicationInfo.BuildVersion);

        Parser.Default.ParseArguments<MigrationProgramConfig>(args)
                      .WithParsedAsync(config => MigrationProgram(config, applicationInfo));
    }

    private static async Task MigrationProgram(MigrationProgramConfig config, ApplicationInfo applicationInfo)
    {
        ServiceProvider = ServiceProviderFactory.CreateServiceProvider(config, applicationInfo); 
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
}