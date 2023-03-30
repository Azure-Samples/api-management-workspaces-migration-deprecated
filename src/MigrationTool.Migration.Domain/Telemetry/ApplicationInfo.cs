using System.Reflection;

namespace MigrationTool
{
    public class ApplicationInfo : IApplicationInfo
    {
        public string Name { get; }
        public string BuildVersion  { get; }

        private ApplicationInfo(string name, string buildVersion)
        {
            Name = name;
            BuildVersion = buildVersion;
        }

        public static ApplicationInfo GetInfo()
        {
            return new ApplicationInfo("azure-api-management-workspaces-migration-tool", Assembly.GetEntryAssembly().GetName().Version.ToString()); 
        }
    }
}