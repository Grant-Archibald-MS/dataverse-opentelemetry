using Microsoft.Xrm.Sdk.PluginTelemetry;
using System;

namespace Microsoft.Dataverse.Samples
{
    /// <summary>
    /// Example that demonstrates the use of Dataverse ILogger in a Dataverse plugin.
    /// This class provides an implementation of a Dataverses out-of-the-box ILogger from Microsoft.Xrm.Sdk.
    /// </summary>
    /// <remarks>https://learn.microsoft.com/en-us/power-apps/developer/data-platform/application-insights-ilogger has more details.</remarks>
    public class DataverseLoggerPlugin : PluginBase
    {
        /// <summary>
        /// Empty Constructor for the DataverseLoggerPlugin class.
        /// </summary>
        /// <param name="unsecureConfiguration">\</param>
        /// <param name="secureConfiguration"></param>
        public DataverseLoggerPlugin(string unsecureConfiguration, string secureConfiguration) : base(typeof(DataverseLoggerPlugin))
        {
            // No configuration needed as ConnectionSTring set at environment level
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
            var pluginLogger = (ILogger)localPluginContext.ServiceProvider.GetService(typeof(ILogger));

            // Get the plugin execution context
            var context = localPluginContext.PluginExecutionContext;

            try
            {
                var mapper = new ContextMapper();
                var defaultValues = mapper.GetDefaultValues(context, out string traceParent);
                
                context.OutputParameters["TraceParent"] = traceParent;

                // Log the message ILogger that needs to be enabled with Environment level configuration
                TraceWithILogger(pluginLogger, defaultValues.Level, defaultValues.Message, traceParent);
            }
            catch (Exception ex)
            {
                context.OutputParameters["TraceParent"] = "ERROR - " + ex.ToString();
            }
        }

        /// <summary>
        /// Logs messages using the out-of-the-box ILogger from Microsoft.Xrm.Sdk.
        /// </summary>
        /// <param name="pluginLogger">ILogger instance from Microsoft.Xrm.Sdk.</param>
        /// <param name="level">Log level for ILogger.</param>
        /// <param name="message">Message to be logged.</param>
        /// <param name="traceParent">Trace parent for distributed tracing.</param>
        private void TraceWithILogger(Xrm.Sdk.PluginTelemetry.ILogger pluginLogger, Xrm.Sdk.PluginTelemetry.LogLevel level, string message, string traceParent)
        {
            var logMessage = string.IsNullOrEmpty(traceParent) ? message : $"{message} - TraceParent: {traceParent}";
            pluginLogger.Log(level, logMessage);
        }
    }
}
