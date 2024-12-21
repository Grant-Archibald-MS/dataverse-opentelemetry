using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;
using System.Diagnostics;

namespace Microsoft.Dataverse.Samples
{
    /// <summary>
    /// Plugin development guide: https://docs.microsoft.com/powerapps/developer/common-data-service/plug-ins
    /// Best practices and guidance: https://docs.microsoft.com/powerapps/developer/common-data-service/best-practices/business-logic/
    /// </summary>
    public class OpenTelemetryPlugin : PluginBase
    {


        public OpenTelemetryPlugin(string unsecureConfiguration, string secureConfiguration) : base(typeof(OpenTelemetryPlugin))
        {
        }

        // Entry point for custom business logic execution
        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;

            try {
                var serviceProvider = localPluginContext.ServiceProvider;

                var connectionString = GetEnvironmentVariable(localPluginContext, "OpenTelemetryConnectionString");

                // validating source input param 
                if (!context.InputParameters.Contains("Source"))
                    throw new InvalidPluginExecutionException($"We couldn't find a valid Source in the context.InputParameters Collection.");

                // validating stage input param 
                if (!context.InputParameters.Contains("Stage"))
                    throw new InvalidPluginExecutionException($"We couldn't find a valid stage in the context.InputParameters Collection.");

                // validating message input param 
                if (!context.InputParameters.Contains("Level"))
                    throw new InvalidPluginExecutionException($"We couldn't find a valid level in the context.InputParameters Collection.");

                // validating message input param 
                if (!context.InputParameters.Contains("Message"))
                    throw new InvalidPluginExecutionException($"We couldn't find a valid message in the context.InputParameters Collection.");

                var source = context.InputParameters["Source"] as string;
                var stage = context.InputParameters["Stage"]  as string;
                var level = LogLevel.Information;
                Enum.TryParse<LogLevel>(context.InputParameters["Level"] as string, out level);
                var message = context.InputParameters["Message"] as string;

                // Create a new tracer provider builder and add an Azure Monitor trace exporter to the tracer provider builder.
                // It is important to keep the TracerProvider instance active throughout the process lifetime.
                // See https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace#tracerprovider-management
                var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                    .AddSource(source)
                    .SetSampler(new AlwaysOnSampler())
                    .AddAzureMonitorTraceExporter(o => o.ConnectionString = connectionString)
                    .Build();

                var tracer = tracerProvider.GetTracer(source);
                
                // Create a new logger factory.
                // It is important to keep the LoggerFactory instance active throughout the process lifetime.
                // See https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/logs#logger-management
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddOpenTelemetry(options =>
                    {
                        options.AddAzureMonitorLogExporter(o => o.ConnectionString = connectionString);
                    });
                    builder.SetMinimumLevel(LogLevel.Debug);
                });

                var logger = loggerFactory.CreateLogger<OpenTelemetryPlugin>();

                ActivityContext parent = new ActivityContext();
                 
                var traceParent = String.Empty;

                if (context.InputParameters.Contains("TraceParent"))
                    traceParent = (string)context.InputParameters["TraceParent"];

                var myActivitySource = new ActivitySource(source);

                if ( !string.IsNullOrEmpty(traceParent) ) {
                    parent = ActivityContext.Parse(traceParent, null);
                   
                    using (var activity = myActivitySource.StartActivity(stage, ActivityKind.Internal, parentContext: parent))
                    {
                        if ( activity != null ) {
                            context.OutputParameters["TraceParent"] = activity.Id.ToString();
                        }
                        
                        // Write a log within the context of an activity
                        logger.Log(level,message);
                    }
                } else {
                    using (var activity = myActivitySource.StartActivity(stage, ActivityKind.Internal))
                    {
                        if ( activity != null ) {
                            context.OutputParameters["TraceParent"] = activity.Id.ToString();
                        }
                        // Write a log within the context of an activity
                        logger.Log(level,message);
                    }
                }

                // Dispose logger factory before the application ends.
                // This will flush the remaining logs and shutdown the logging pipeline.
                loggerFactory.Dispose();

                // Dispose tracer provider before the application ends.
                // This will flush the remaining spans and shutdown the tracing pipeline.
                tracerProvider.Dispose();
            } catch ( Exception ex ) {
                context.OutputParameters["TraceParent"] = "ERROR - " + ex.ToString();
            }
        }

        private string GetEnvironmentVariable(ILocalPluginContext localPluginContext, string variableName) {
            var query = new QueryExpression("environmentvariablevalue");
            query.ColumnSet.AddColumns("value");

            // Link to the environment variable definition based on display name
            var query_environmentvariabledefinition = query.AddLink(
                "environmentvariabledefinition",
                "environmentvariabledefinitionid",
                "environmentvariabledefinitionid"
            );
            query_environmentvariabledefinition.LinkCriteria.AddCondition(
                "displayname",
                ConditionOperator.Equal,
                variableName
            );

            var service = localPluginContext.OrgSvcFactory.CreateOrganizationService(localPluginContext.PluginExecutionContext.UserId);

            // Execute the query
            EntityCollection results = service.RetrieveMultiple(query);

            // Retrieve the value (assuming there's only one result)
            if (results.Entities.Count == 1)
            {
                return results.Entities[0].GetAttributeValue<string>("value");
   
            }

            return String.Empty;
        }
    }
}
