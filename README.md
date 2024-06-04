# Dataverse Open Telemetry sample

This project contains an example of Dataverse plugin integration with [Open Telemetry](https://opentelemetry.io/). OpenTelemetry is a collection of APIs, SDKs, and tools. Use it to instrument, generate, collect, and export telemetry data (metrics, logs, and traces) to help you analyze your software’s performance and behavior.

## Goals

The design goals of this sample are to:

1. Demonstrate how the Dataverse plugin can be used to implement distributed tracing across Power Platform components

2. Show out the out of the box Azure Monitor Trace Exporter can use used to generate trace and dependency data

3. Show how the plugin can be used across Power Apps, Power Automate and Dataverse to provide end to end to traceability.

4. Use of Power Platform Environment variables to configure Connection parameters.

## Future

The following area could be expanded on this sample to demonstrate other scenarios. This could include:

1. Implement a Dataverse Exporter using the [Extending the sdk](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace/extending-the-sdk#exporter) guidance to shows how monitoring data could be stored in Dataverse or other locations.

2. Use of Power Platform Environment variables which exporters should be used e.g. Azure Monitor, OLTP exporter or Dataverse Exporter.

3. Demonstrate how to use customer managed key (CMK) to encrypt telemetry.

## Architecture

Visualizing the end to end process could be shown in the following diagram

![End to end summary showing instrument Power Platform components using Trace Parent, Data verse plugin and Azure Monitor exporter](./docs/media/overview.png)

Power Platform components like Power Apps, Power Automate, Dataverse Plugins and Microsoft Co-Pilot studio can be instrumented by calling the Dataverse Plugin.

The plugin queries the Dataverse Environment for the connection string. The plugin then starts a unique operation will call the Dataverse Plugin which will return a unique Trace Parent consisting of a Trace Id and Span Id. Following calls pass in the initial Trace Parent, which will continue the same operation id and generate a new span id.

The Dataverse Plugin uses the .Net NuGet Open Telemetry provider to use the default Azure Monitor Exporter to generate trace and dependency records in Application Insights.

## Trace Parent Relationship

The following table demonstrates the Trace Parent where the same operation id persists and each component has a unique id appended which can ue sued to regenerate parent child relationship between components.

| Component      | Trace Parent (In)                                    | Trace Parent Out                                     | Message |
|----------------|------------------------------------------------------|------------------------------------------------------|---------|
| Power App      |                                                      | 00-2d708f92ccca7d80ca0ba94bfc97f32a-df0cb752131c1561 | Application Started |
| Power App      | 00-2d708f92ccca7d80ca0ba94bfc97f32a-df0cb752131c1561 | 00-2d708f92ccca7d80ca0ba94bfc97f32a-015c4c32ecb15800 | Button Clicked |
| Power Automate | 00-2d708f92ccca7d80ca0ba94bfc97f32a-015c4c32ecb15800 | 00-2d708f92ccca7d80ca0ba94bfc97f32a-4022c655c6549306 | Flow started |
| Power Automate | 00-2d708f92ccca7d80ca0ba94bfc97f32a-4022c655c6549306 | 00-2d708f92ccca7d80ca0ba94bfc97f32a-c7d2d09caadb0463 | Child Flow started |

## Example

Demonstrating an example of this relationships with a Power App that calls a Power Automate cloud flow and the resulting Application Insights, traces and dependency.

![Power Apps Test Application - Not started](./docs/media/01-sample-power-app-start.png)

The test Power App has no Trace Parent when loaded. The user can then select the Send button. This action will call the Dataverse plugin. 

![Power Apps Test Application - Started](./docs/media/02-PowerApp-Step1.png)

The Trace Parent updated as a result of the first span in the process.

![Power Automate Workflow called](./docs/media/03-Workflow-Called.png)

After the Power Automate Cloud flow it generates a new span id for the cloud flow action as part of the same operation id

![Application Insights Transaction Search](./docs/media/04-ApplicationInsights-TransactionSearch.png)

The operation id that relates all these steps can be queries in Application Insights Transaction search

![Application Insights Transaction Details](./docs/media/05-ApplicationInsights-TransactionDetails.png)

And the details and relationship between the components be visualized.
