using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Dataverse.Samples
{
    /// <summary>
    /// Example that demonstrates the use of Application Insights in a Dataverse plugin.
    /// This class provides an implementation of a Dataverse plugin that utilizes both the out-of-the-box ILogger from Microsoft.Xrm.Sdk and Application Insights for enhanced telemetry capabilities.
    /// </summary>
    public class ApplicationInsightsPlugin : PluginBase
    {
        // Connection string for Application Insights
        private string connectionString;
        // Default log level for Application Insights
        private LogLevel defaultLogLevel = LogLevel.Information;
        // Flag to enable or disable Application Insights
        private bool _enabled = false;

        /// <summary>
        /// Constructor for the ApplicationInsightsPlugin class.
        /// </summary>
        /// <param name="unsecureConfiguration">Unsecure configuration string, expected to be JSON.</param>
        /// <param name="secureConfiguration">Secure configuration string, typically the connection string.</param>
        public ApplicationInsightsPlugin(string unsecureConfiguration, string secureConfiguration) : base(typeof(ApplicationInsightsPlugin))
        {
            this.connectionString = secureConfiguration;

            // Deserialize the unsecure configuration if it is not null or empty
            if (!string.IsNullOrEmpty(unsecureConfiguration))
            {
                var config = JsonSerializer.Deserialize<ApplicationInsightsConfig>(unsecureConfiguration);
                if (config != null)
                {
                    _enabled = config.Enabled;
                    defaultLogLevel = Enum.TryParse(config.LogLevel, out LogLevel logLevel) ? logLevel : LogLevel.Information;
                }
            }
        }

        /// <summary>
        /// Entry point for custom business logic execution.
        /// </summary>
        /// <param name="localPluginContext">Context for the plugin execution.</param>
        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            // Get the ILogger instance from Microsoft.Xrm.Sdk
            var pluginLogger = (Xrm.Sdk.PluginTelemetry.ILogger)localPluginContext.ServiceProvider.GetService(typeof(Xrm.Sdk.PluginTelemetry.ILogger));

            // Get the plugin execution context
            var context = localPluginContext.PluginExecutionContext;

            try
            {
                // Extract input parameters from the context
                var source = context.InputParameters["Source"] as string;
                var stage = context.InputParameters["Stage"] as string;
                var pluginLevel = Xrm.Sdk.PluginTelemetry.LogLevel.Information;
                Enum.TryParse(context.InputParameters["Level"] as string, out pluginLevel);
                var message = context.InputParameters["Message"] as string;
                var traceParent = context.InputParameters.Contains("TraceParent") ? (string)context.InputParameters["TraceParent"] : string.Empty;

                // Check if the trace parent is in the shared variables
                if (string.IsNullOrEmpty(traceParent)) {
                    if (context.SharedVariables.ContainsKey("tag") && !string.IsNullOrEmpty(context.SharedVariables["tag"] as string))
                    {
                        traceParent = context.SharedVariables["tag"] as string;
                    }
                }

                context.OutputParameters["TraceParent"] = traceParent;

                // Log the message using both Application Insights and ILogger
                LogWithApplicationInsightsAsync(source, stage, pluginLevel, message, traceParent, context);
                TraceWithILoggerAsync(pluginLogger, pluginLevel, message, traceParent);
            }
            catch (Exception ex)
            {
                context.OutputParameters["TraceParent"] = "ERROR - " + ex.ToString();
            }
        }

        /// <summary>
        /// Logs messages using Application Insights.
        /// </summary>
        /// <param name="source">Source of the telemetry data.</param>
        /// <param name="stage">Stage of the telemetry data.</param>
        /// <param name="level">Log level for Application Insights.</param>
        /// <param name="message">Message to be logged.</param>
        /// <param name="traceParent">Trace parent for distributed tracing.</param>
        /// <param name="context">Plugin execution context.</param>
        private void LogWithApplicationInsightsAsync(string source, string stage, Xrm.Sdk.PluginTelemetry.LogLevel level, string message, string traceParent, IPluginExecutionContext context)
        {
            // Check if Application Insights is enabled
            if (!_enabled)
            {
                return;
            }

            // Check if the log level is greater than or equal to the default log level
            if (level < this.defaultLogLevel)
            {
                return;
            }

            // Create a telemetry configuration and client for Application Insights
            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.ConnectionString = connectionString;
            var telemetryClient = new TelemetryClient(telemetryConfiguration);

            // If a trace parent is provided, create a dependency record and link the trace as a child
            if (!string.IsNullOrEmpty(traceParent))
            {
                // Create a dependency telemetry record
                var dependencyTelemetry = new DependencyTelemetry
                {
                    Name = stage,
                    Target = source,
                    Type = "Custom",
                    Timestamp = DateTime.UtcNow,
                    Duration = TimeSpan.Zero,
                    Success = true,
                    Context = { Operation = { ParentId = traceParent, Id = Activity.Current?.Id ?? Guid.NewGuid().ToString() } }
                };

                // Track the dependency telemetry
                telemetryClient.TrackDependency(dependencyTelemetry);

                // Create a new activity for the trace message
                var activity = new Activity("CustomActivity");
                activity.SetParentId(traceParent);
                activity.Start();

                // Create a trace telemetry record
                var traceTelemetry = new TraceTelemetry(message, ConvertLogLevel(level))
                {
                    Message = message,
                    Context = { Operation = { ParentId = dependencyTelemetry.Id, Id = activity.Id } }
                };

                // Update the TraceParent output parameter with the new activity ID
                context.OutputParameters["TraceParent"] = activity.Id;

                // Track the trace telemetry
                telemetryClient.TrackTrace(traceTelemetry);
            }
            else
            {
                // If no trace parent is provided, start a new activity and create a trace record
                var activity = new Activity("CustomActivity");
                activity.Start();

                // Create a trace telemetry record
                var traceTelemetry = new TraceTelemetry(message, ConvertLogLevel(level))
                {
                    Message = message,
                    Context = { Operation = { ParentId = activity.Id, Id = activity.Id } }
                };

                // Update the TraceParent output parameter with the new activity ID
                context.OutputParameters["TraceParent"] = activity.Id;

                // Track the trace telemetry
                telemetryClient.TrackTrace(traceTelemetry);
            }

            // Flush the telemetry client to ensure all data is sent
            telemetryClient.Flush();
        }


        /// <summary>
        /// Converts the Xrm.Sdk.PluginTelemetry.LogLevel to Application Insights SeverityLevel.
        /// </summary>
        /// <param name="level">Log level from Xrm.Sdk.PluginTelemetry.</param>
        /// <returns>Converted SeverityLevel for Application Insights.</returns>
        private SeverityLevel ConvertLogLevel(Xrm.Sdk.PluginTelemetry.LogLevel level)
        {
            switch (level)
            {
                case Xrm.Sdk.PluginTelemetry.LogLevel.Critical:
                    return SeverityLevel.Critical;
                case Xrm.Sdk.PluginTelemetry.LogLevel.Error:
                    return SeverityLevel.Error;
                case Xrm.Sdk.PluginTelemetry.LogLevel.Warning:
                    return SeverityLevel.Warning;
                case Xrm.Sdk.PluginTelemetry.LogLevel.Information:
                    return SeverityLevel.Information;
                default:
                    return SeverityLevel.Information;
            }
        }

        /// <summary>
        /// Logs messages using the out-of-the-box ILogger from Microsoft.Xrm.Sdk.
        /// </summary>
        /// <param name="pluginLogger">ILogger instance from Microsoft.Xrm.Sdk.</param>
        /// <param name="level">Log level for ILogger.</param>
        /// <param name="message">Message to be logged.</param>
        /// <param name="traceParent">Trace parent for distributed tracing.</param>
        private void TraceWithILoggerAsync(Xrm.Sdk.PluginTelemetry.ILogger pluginLogger, Xrm.Sdk.PluginTelemetry.LogLevel level, string message, string traceParent)
        {
            var logMessage = string.IsNullOrEmpty(traceParent) ? message : $"{message} - TraceParent: {traceParent}";
            pluginLogger.Log(level, logMessage);
        }
    }
}
