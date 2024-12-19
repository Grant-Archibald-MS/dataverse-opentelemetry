# Dataverse Plugin: Passing Information to Plugin Execution

## Introduction

In Dataverse, plugins are used to extend the functionality of the platform by executing custom business logic. Plugins run in a sandbox environment, which ensures that they are isolated and secure. However, there are scenarios where you need to pass additional information to the plugin execution context. One way to achieve this is by using the `tag` attribute.

This sample explains the need to pass information to plugin execution and demonstrates how to use the `tag` attribute to share context with the plugin. We will use a sample application located in `src\DataversePlugin` that appends the supplied tag to the end of the account name. More information on [Add a shared variable to the plugin execution context](https://learn.microsoft.com/power-apps/developer/data-platform/optional-parameters?view=dataverse-latest&tabs=sdk#add-a-shared-variable-to-the-plugin-execution-context) using SDK.Net and WebApi.

## Why Pass Information to Plugin Execution?

Passing information to plugin execution is essential for several reasons:

1. **Custom Business Logic**: Plugins often need additional context to execute custom business logic. For example, you might want to apply specific rules or transformations based on the provided context.
2. **Data Enrichment**: By passing additional information, you can enrich the data being processed by the plugin. This can be useful for logging, auditing, or enhancing the data before it is stored in Dataverse.
3. **Dynamic Behavior**: Plugins can behave dynamically based on the provided context. This allows for more flexible and adaptable business processes.

This sample could be the basis to pass through TraceParent as described in [Power Platform Distributed Tracing](./PowerPlatformDistributedTracing.md) as an example of how to pass context to where context starts from code first tools interacting with Dataverse.

## Using the `tag` Attribute

The `tag` attribute is a simple way to pass additional information to the plugin execution context. In this example, we will demonstrate how to use the `tag` attribute to append a tag to the account name.

### Sample Code

The sample plugin is located in [src/DataversePlugin](../src/DataversePlugin/CreateAccountPlugin.cs) and is a simple plugin that appends the supplied tag to the end of the account name. 

The [src/client/Program.cs](../src/client/Program.cs) show the call using the Microsoft.PowerPlatform.Dataverse.Client and Microsoft.Xrm.Sdk to Create a new account entity.

## Getting Started

1. Download and install .Net 4.6.4 SDK [Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net462)

2. Follow instructions to [Install .Net On Windows](https://learn.microsoft.com/dotnet/core/install/windows)

3. Install the Power Platform CLI (pac) if you haven't already:

```pwsh
dotnet tool install -g Microsoft.PowerPlatform.Cds.Client
```

4. Ensure you have the minimum permissions required to install a Dataverse plugin package using the pack tool. You need to have the following permissions:

- System Administrator or System Customizer role in your Power Platform environment.
- Access to the Dataverse environment where you want to install the plugin package.

5. Follow the steps of DataversePlugin [README](../src/DataversePlugin/README.md) to build and register the plugin and plugin step

6. Follow the steps of client [README](../src/client/README.md) to run the sample .Net console application to insert the sample account.
