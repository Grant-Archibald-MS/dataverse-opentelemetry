using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace Microsoft.Dataverse.Samples
{
    /// <summary>
    /// Example that demonstrates the use of Application Insights in a Dataverse plugin.
    /// This class provides an implementation of a Dataverse plugin that created Dependancy and Trace messaged in Application Insights for configurable telemetry capabilities.
    /// </summary>
    public class ApplicationInsightsPlugin : PluginBase
    {
        // Connection string for Application Insights
        private string connectionString;
        // Default log level for Application Insights
        private LogLevel defaultLogLevel = LogLevel.Information;
        // Flag to enable or disable Application Insights
        private bool _enabled = false;
        private string outputFieldName = "TraceParent";

        private bool _append = false;

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
                    if (!string.IsNullOrEmpty(config.OutputField))
                    {
                        outputFieldName = config.OutputField;
                    }
                    _append = config.Append;
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

            // Get the plugin execution context
            var context = localPluginContext.PluginExecutionContext;

            try
            {
                var mapper = new ContextMapper();
                var defaultValues = mapper.GetDefaultValues(context, out string traceParent);

                // Log the message using both Application Insights
                LogWithApplicationInsightsAsync(defaultValues.Source, defaultValues.Stage, defaultValues.Level, defaultValues.Message, traceParent, context);
            }
            catch (Exception ex)
            {
                context.OutputParameters[outputFieldName] = "ERROR - " + ex.ToString();
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

                SetOutputValue(context, activity.Id);

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
                SetOutputValue(context, activity.Id);

                // Track the trace telemetry
                telemetryClient.TrackTrace(traceTelemetry);
            }

            // Flush the telemetry client to ensure all data is sent
            telemetryClient.Flush();
        }

        /// <summary>
        /// Sets the output value for the TraceParent field in the plugin execution context.
        /// </summary>
        /// <param name="context">The current context to add the variables</param>
        /// <param name="activityId">The new activity id</param>
        private void SetOutputValue(IPluginExecutionContext context, string activityId)
        {
            if (context.OutputParameters.Contains(outputFieldName))
            {
                if (_append)
                {
                    context.OutputParameters[outputFieldName] += " " + activityId;
                }
                else
                {
                    context.OutputParameters[outputFieldName] = activityId;
                }
            }
            else
            {
                context.OutputParameters[outputFieldName] = activityId;
            }
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
    }
}
