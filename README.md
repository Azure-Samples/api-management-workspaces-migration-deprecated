# Azure API Management workspaces migration tool

**This repository is no longer maintained. The tool hasn't been updated to migrate resource types introduced in workspaces since the release of the tool or to account for [the new workspaces data model](https://learn.microsoft.com/azure/api-management/breaking-changes/workspaces-breaking-changes-june-2024)."**

With the Azure API Management workspaces migration tool, you can migrate selected service-level APIs with their dependencies from an Azure API Management instance to an Azure API Management [workspace](https://learn.microsoft.com/azure/api-management/workspaces-overview). 

The tool is in active development while workspaces are in public preview. Currently, the tool only copies selected resources from the service level to a workspace. All original resources are maintained at the service level, to prevent impact to the API Management instance. 

We encourage customers with workspaces to use and test the tool and we welcome your feedback. See [Support and feedback](#support-and-feedback).  

## Supported dependencies 

For APIs that you select, the tool detects the following dependencies and copies them to a workspace: 

- API version sets, APIs, and all child resources, including versions, revisions, and policies 

- Named values 

- Products 

- Subscriptions 

The tool aborts migration if it detects the following scenarios that aren't supported in workspaces preview: 

* A backend used in the `set-backend` policy 

* Assignment of an API to a self-hosted gateway 

Other dependent resources aren't currently supported but are planned for future support. See [Roadmap](#roadmap).

## Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) 
`
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) for authentication 

- An API Management instance with an existing [workspace](https://learn.microsoft.com/azure/api-management/how-to-create-workspace) 

- Permissions
    - Read access for API Management service-level APIs and [supported dependencies](#supported-dependencies) that can be migrated 
    - Write access for APIs and all [supported dependencies](#supported-resource-types-and-dependencies) in the workspace
    
## Run the tool

1. Open a terminal window. Log in with Azure CLI and, if needed, set the correct Azure subscription: 

    ```  
    az login
    az account set <your subscription ID>
    ``` 

1. Clone the repo and its submodules locally.

    ```
    git clone --recurse-submodules https://github.com/Azure-Samples/api-management-workspaces-migration.git
    ```

1. Change directory to the `api-management-workspaces-migration` directory and build the tool:

    ```
    cd api-management-workspaces-migration
    dotnet build MigrationTool.sln
    ``` 

1. Run the tool, passing parameters for the names of the resource group and the API Management instance in which to perform migration:

    ```
    dotnet run --project .\src\MigrationTool.Runtime\MigrationTool.Runtime.csproj --resourceGroup <resource group name> --serviceName <API Management instance name>
    ```

1. From the list that's displayed, select the APIs or API version sets to migrate. 

    You should manually check if a selected API uses any unsupported dependencies before attempting the migration; if it does, your API can't be migrated with the tool.

1. Select a workspace to migrate the resources to. 

1. The tool analyzes dependencies of selected APIs, like named values or subscriptions, and generates a migration plan. See [Supported dependencies](#supported-dependencies) for the list of resources supported in the tool and [Roadmap](#roadmap) for the list of resources that are planned for future support. 
    
    If the tool detects a dependency of a type that isn't supported in workspaces, it will automatically abort the migration attempt. 
    
1. When prompted, confirm to execute the migration. 

1. The tool copies the selected APIs, API version sets, and revisions into the selected workspace. It computes a random hash and appends it to the API name and API suffix to avoid conflicts. 

    For each detected API dependency: 

    * If the dependency is used *only* by APIs that are being migrated to the workspace, the tool copies it to the workspace with the random hash appended to its name. In the future, the original resource on the service level will be deleted. While in preview, the tool keeps all the original resources for backup.

    * If the dependency is used also by other APIs and it's of a type that allows referencing a service-level resource from a workspace-level resource (that is, product or tag), this dependency along with all of its dependencies (for example, subscription to a product or tag) will remain on the service level and the migrated API will reference it from a workspace. 

    * If the dependency is used also by other APIs and doesn't allow referencing a service-level resource from a workspace-level resource, this dependency will be copied to a workspace with the random hash appended to its name. The future versions of the tool will still leave it undeleted.

    Note: Migrated APIs in a workspace are updated to reference any dependency that's migrated to the workspace. For example, if a dependent product is migrated, the product assignment in a migrated API is updated to point to the migrated product instead.

## Clean up resources

You can manually delete the original resources and change the API URL suffix to enable a migrated API. This operation may cause several minutes of downtime for the migrated API.

In the future, after preview versions, the tool will automatically clean up resources: 

* Delete the original APIs and the dependencies that were marked for deletion. 

* Remove the suffix in the API URL path and from the name of resources whose original versions were deleted from the service level. 

## Roadmap 

We plan to add detection and migration of the following resource types that aren't currently supported:

- Tags* 

- Groups* 

- Policy fragments 

- Schemas (`validate-content` policy) 

We plan to add detection of the following resource types that aren't supported in workspaces to abort the migration: 

- Certificates* 

- External cache* 

- Authorization servers* 

- Loggers 

- Authorizations 

Resources annotated with * will be prioritized for the upcoming tool releases.

The tool may be incorporated into the API Management’s management API and Azure portal interfaces for GA release, subject to customer feedback and technical feasibility.

## Support and feedback

We provide support through GitHub [Issues] and [Discussions] only. There is no paid support channel for this tool.

Report bugs or submit feature requests in GitHub [Issues]. Please use one of the provided templates so that we gather all appropriate information.

## Telemetry

The migration tool sends user-agent data to Azure to measure usage of the tool and to help with improvements.

The user-agent contains only the version of the migration tool that is used.

## License

This project is licensed under [the MIT License](LICENSE)

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

<!-- Links -->
[Issues]: https://github.com/Azure-Samples/api-management-workspaces-migration/issues
[Discussions]: https://github.com/Azure-Samples/api-management-workspaces-migration/discussions
