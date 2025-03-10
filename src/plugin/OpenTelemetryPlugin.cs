using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Dataverse.Samples
{
    /// <summary>
    /// Example that demonstrates the use of OpenTelemetry in a Dataverse plugin.
    /// 
    /// This class provides an implementation of a Dataverse plugin that utilizes both the out-of-the-box ILogger from Microsoft.Xrm.Sdk and the optional OpenTelemetry for enhanced telemetry capabilities.
    /// 
    /// Key Concepts:
    /// 1. ILogger from Microsoft.Xrm.Sdk: This logger is used for logging within Dataverse plugins. 
    ///    Telemetry is managed by the service, and telemetry data is configured at the environment level. 
    ///    This logger is used to log messages related to plugin execution and is integrated with the Dataverse environment.
    /// 2. ILogger from Microsoft.Extensions.Logging: This logger is used for logging with OpenTelemetry.
    ///    In this example OpenTelemetry is an optional add-on that provides enhanced telemetry capabilities.
    ///    By using OpenTelemetry, it inserts the dependency record and makes use of the W3C standard for distributed tracing to ensure that data can be correlated across different services and components.
    /// 3. OpenTelemetry: OpenTelemetry provides a way to collect and export telemetry data, such as traces, metrics, and logs. It is configured using the connection string provided in the secure configuration. OpenTelemetry enhances the observability of the application by providing detailed insights into its behavior and performance.
    /// 4. Distributed Tracing: Distributed tracing allows tracking the flow of requests across different services and components.
    ///    OpenTelemetry uses the W3C standard for distributed tracing, which ensures that trace data can be correlated across different systems.
    /// 5. Why OpenTelemetry SDK is used instead of ApplicationInsights SDK: OpenTelemetry provides vendor-neutral instrumentation, which means it can be used with multiple monitoring solutions, not just ApplicationInsights.
    ///    It supports more metric instruments, including histograms, and includes Redis instrumentations.
    ///    OpenTelemetry is generally more performant at scale and capable of exporting to multiple destinations.
    ///    This flexibility and enhanced performance make it a preferred choice for modern applications.
    ///
    /// 
    /// Comparison of Methods:
    /// - TraceWithILoggerAsync:
    ///      This method logs messages using the out-of-the-box ILogger from Microsoft.Xrm.Sdk. 
    ///      It is used when OpenTelemetry is not enabled or the connection string is not provided.
    ///      This method ensures that telemetry data is managed by the Dataverse service and configured at the environment level.
    /// - LogWithOpenTelemetryAsync: 
    ///      This method logs messages using OpenTelemetry.
    ///      It is used when OpenTelemetry is enabled and the connection string is provided.
    ///      This method enhances the observability of the application by providing detailed insights into its behavior and performance. It also supports distributed tracing to correlate data across different services and components.
    /// </summary>
    public class OpenTelemetryPlugin : PluginBase
    {
        // Connection string for OpenTelemetry
        private string connectionString;
        // Default log level for OpenTelemetry
        private LogLevel defaultLogLevel = LogLevel.Information;
        // Flag to enable or disable OpenTelemetry
        private bool enableOpenTelemetry = false;

        /// <summary>
        /// Constructor for the OpenTelemetryPlugin class.
        /// </summary>
        /// <param name="unsecureConfiguration">Unsecure configuration string, expected to be JSON.</param>
        /// <param name="secureConfiguration">Secure configuration string, typically the connection string.</param>
        public OpenTelemetryPlugin(string unsecureConfiguration, string secureConfiguration) : base(typeof(OpenTelemetryPlugin))
        {
            this.connectionString = secureConfiguration;

            // Deserialize the unsecure configuration if it is not null or empty
            if (!string.IsNullOrEmpty(unsecureConfiguration))
            {
                var config = JsonSerializer.Deserialize<OpenTelemetryConfig>(unsecureConfiguration);
                if (config != null)
                {
                    defaultLogLevel = Enum.TryParse(config.LogLevel, out LogLevel logLevel) ? logLevel : LogLevel.Information;
                    enableOpenTelemetry = config.EnableOpenTelemetry;
                }
            }
        }

        /// <summary>
        /// Entry point for custom business logic execution.
        /// </summary>
        /// <param name="localPluginContext">Context for the plugin execution.</param>
        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            ExecuteDataversePluginAsync(localPluginContext).Wait();
        }

        /// <summary>
        /// Asynchronous method for executing the Dataverse plugin.
        /// </summary>
        /// <param name="localPluginContext">Context for the plugin execution.</param>
        private async Task ExecuteDataversePluginAsync(ILocalPluginContext localPluginContext) { 
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            // Get the ILogger instance from Microsoft.Xrm.Sdk
            var pluginLogger = (Xrm.Sdk.PluginTelemetry.ILogger)localPluginContext.ServiceProvider.GetService(typeof(Xrm.Sdk.PluginTelemetry.ILogger));

            var context = localPluginContext.PluginExecutionContext;

            try {
                var serviceProvider = localPluginContext.ServiceProvider;

                // Validate input parameters
                if (!context.InputParameters.Contains("Source"))
                    throw new InvalidPluginExecutionException($"We couldn't find a valid Source in the context.InputParameters Collection.");

                if (!context.InputParameters.Contains("Stage"))
                    throw new InvalidPluginExecutionException($"We couldn't find a valid stage in the context.InputParameters Collection.");

                if (!context.InputParameters.Contains("Level"))
                    throw new InvalidPluginExecutionException($"We couldn't find a valid level in the context.InputParameters Collection.");

                if (!context.InputParameters.Contains("Message"))
                    throw new InvalidPluginExecutionException($"We couldn't find a valid message in the context.InputParameters Collection.");

                var traceParent = String.Empty;

                if (context.InputParameters.Contains("TraceParent"))
                    traceParent = (string)context.InputParameters["TraceParent"];

                if (string.IsNullOrEmpty(traceParent) && context.SharedVariables.ContainsKey("tag"))
                {
                    // TraceParent is empty but a tag has been provided, assume that the tag is the W3C traceparent
                    traceParent = context.SharedVariables["tag"].ToString();
                }

                context.OutputParameters["TraceParent"] = traceParent;

                var source = context.InputParameters["Source"] as string;
                var stage = context.InputParameters["Stage"]  as string;
                var openTelemetryLevel = LogLevel.Information;
                Enum.TryParse(context.InputParameters["Level"] as string, out openTelemetryLevel);
                var pluginLevel =  Xrm.Sdk.PluginTelemetry.LogLevel.Information;
                Enum.TryParse(context.InputParameters["Level"] as string, out pluginLevel);
                var message = context.InputParameters["Message"] as string;

                var tasks = new List<Task>();

                // If OpenTelemetry is enabled and connection string is provided, log with OpenTelemetry
                if (enableOpenTelemetry && !string.IsNullOrEmpty(connectionString))
                { 
                    tasks.Add(LogWithOpenTelemetryAsync(source, stage, openTelemetryLevel, message, traceParent, context));
                    tasks.Add(TraceWithILoggerAsync(pluginLogger, pluginLevel, message, traceParent));
                }
                else
                {
                    // Otherwise, log with the out-of-the-box ILogger only
                    tasks.Add(TraceWithILoggerAsync(pluginLogger, pluginLevel, message, traceParent));
                }

                await Task.WhenAll(tasks);

                if (enableOpenTelemetry)
                {
                    DisposeFactoryItems();
                }

            } catch ( Exception ex ) {
                context.OutputParameters["TraceParent"] = "ERROR - " + ex.ToString();
            }
        }

        /// <summary>
        /// Logs messages using OpenTelemetry.
        /// </summary>
        /// <param name="source">Source of the telemetry data.</param>
        /// <param name="stage">Stage of the telemetry data.</param>
        /// <param name="level">Log level for OpenTelemetry.</param>
        /// <param name="message">Message to be logged.</param>
        /// <param name="traceParent">Trace parent for distributed tracing.</param>
        /// <returns>List of tasks for logging.</returns>
        private async Task LogWithOpenTelemetryAsync(string source, string stage, LogLevel level, string message, string traceParent, IPluginExecutionContext context)
        {
            // Validate connection string format
            if (string.IsNullOrEmpty(connectionString) || !connectionString.Contains("InstrumentationKey") || !connectionString.Contains("IngestionEndpoint"))
            {
                throw new InvalidOperationException("Invalid connection string format. Ensure that the connection string includes InstrumentationKey and IngestionEndpoint.");
            }

            // Create a new tracer provider builder and add an Azure Monitor trace exporter to the tracer provider builder.
            // It is important to keep the TracerProvider instance active throughout the process lifetime.
            // See https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/trace#tracerprovider-management
            using( var tracerProvider = CreateTracerProvider(source) ) {

                var tracer = tracerProvider.GetTracer(source);
            
                // Create a new logger factory.
                // It is important to keep the LoggerFactory instance active throughout the process lifetime.
                // See https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/docs/logs#logger-management
                using (var loggerFactory = CreateLoggerFactory(source)) 
                {
                    var logger = loggerFactory.CreateLogger<OpenTelemetryPlugin>();

                    if (logger.IsEnabled(LogLevel.Information)) {
                        logger.LogInformation($"OpenTelemetry logging is enabled. Source: {source}, Stage: {stage}, Level: {level}, Message: {message}");
                    }

                    ActivityContext parent = new ActivityContext();

                    var myActivitySource = new ActivitySource(source);

                    ValidateSetup(source, myActivitySource, tracerProvider, loggerFactory, logger);

                    // Start activity for distributed tracing
                    if (!string.IsNullOrEmpty(traceParent))
                    {
                        parent = ActivityContext.Parse(traceParent, null);

                        // Add this as child of existing
                        using (var activity = myActivitySource.StartActivity(stage, ActivityKind.Internal, parentContext: parent))
                        {
                            if (activity == null)
                            {
                                throw new InvalidOperationException("Activity is null. Ensure that there are active listeners and that the sampling decision allows the activity to be created.");
                            }

                            await LogOpenTelemetryAsync(logger, level, message);
                        }
                    }
                    else
                    {
                        // Start a new distribute tracing session
                        using (var activity = myActivitySource.StartActivity(stage, ActivityKind.Internal))
                        {
                            if (activity == null)
                            {
                                throw new InvalidOperationException("Activity is null. Ensure that there are active listeners and that the sampling decision allows the activity to be created.");
                            }
                            if (activity != null)
                            {
                                // Add this activity as TraceParent for the next action
                                context.OutputParameters["TraceParent"] = activity.Id.ToString();
                                await LogOpenTelemetryAsync(logger, level, message);
                            }
                        }
                    }
                }
            }
        }

        private TracerProvider CreateTracerProvider(string source)
        {
            return Sdk.CreateTracerProviderBuilder()
                .AddSource(source)
                .AddAzureMonitorTraceExporter(o => o.ConnectionString = connectionString)
                .SetSampler(new AlwaysOnSampler())
                .Build();
        }

        private ILoggerFactory CreateLoggerFactory(string source)
        {
            return LoggerFactory.Create(builder =>
                {
                    builder.AddOpenTelemetry(options =>
                    {
                        options.AddAzureMonitorLogExporter(o => o.ConnectionString = connectionString);
                    });
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
        }

        private void ValidateSetup(string source, ActivitySource myActivitySource, TracerProvider tracerProvider, ILoggerFactory loggerFactory, ILogger<OpenTelemetryPlugin> logger)
        {
            // Extra checks to verify why activity might be null
            if (myActivitySource == null)
            {
                throw new InvalidOperationException("ActivitySource is null. Ensure that the source name is correct and that the ActivitySource is properly initialized.");
            }

            if (tracerProvider == null)
            {
                throw new InvalidOperationException("TracerProvider is null. Ensure that the TracerProvider is properly initialized.");
            }

            if (loggerFactory == null)
            {
                throw new InvalidOperationException("LoggerFactory is null. Ensure that the LoggerFactory is properly initialized.");
            }

            if (logger == null)
            {
                throw new InvalidOperationException("Logger is null. Ensure that the Logger is properly initialized.");
            }

            // Check for active listeners
            if (!myActivitySource.HasListeners())
            {
                var redactedConnectionString = RedactConnectionString(connectionString);
                var stateInfo = new
                {
                    ConnectionString = redactedConnectionString,
                    Source = source,
                    TracerProviderInitialized = tracerProvider != null,
                    LoggerFactoryInitialized = loggerFactory != null,
                    LoggerInitialized = logger != null,
                    HasListeners = myActivitySource.HasListeners(),
                    ActivitySourceInfo = GetActivitySourceInfo(myActivitySource),
                    TracerProviderInfo = GetTracerProviderInfo(tracerProvider),
                    LoggerFactoryInfo = GetLoggerFactoryInfo(loggerFactory),
                    EnvironmentInfo = GetEnvironmentInfo(),
                    ThreadInfo = GetThreadInfo()

                };

                throw new InvalidOperationException($"No active listeners found. Ensure that there are active listeners for the activity. State Info: {JsonSerializer.Serialize(stateInfo)}");
            }
        }

        private string RedactConnectionString(string connectionString)
        {
            // Redact sensitive information in the connection string
            var parts = connectionString.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                var namevalue = parts[i].Split('=');
                parts[i] = namevalue[0] + "=REDACTED";
            }
            return string.Join(";", parts);
        }

        private object GetActivitySourceInfo(ActivitySource activitySource)
        {
            return new
            {
                Name = activitySource.Name,
                Version = activitySource.Version,
                HasListeners = activitySource.HasListeners()
            };
        }

        private object GetTracerProviderInfo(TracerProvider tracerProvider)
        {
            return new
            {
                SamplerType = tracerProvider.GetType().Name
            };
        }

        private object GetLoggerFactoryInfo(ILoggerFactory loggerFactory)
        {
            return new
            {
                ProvidersCount = loggerFactory.CreateLogger<OpenTelemetryPlugin>().GetType().Name
            };
        }

        private object GetEnvironmentInfo()
        {
            return new
            {
                OSVersion = Environment.OSVersion.ToString(),
                RuntimeVersion = Environment.Version.ToString(),
                EnvironmentVariables = Environment.GetEnvironmentVariables()
            };
        }

        private object GetThreadInfo()
        {
            return new
            {
                ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
                IsThreadPoolThread = System.Threading.Thread.CurrentThread.IsThreadPoolThread,
                IsBackground = System.Threading.Thread.CurrentThread.IsBackground
            };
        }

        /// <summary>
        /// Disposes the factory items for OpenTelemetry.
        /// </summary>
        private void DisposeFactoryItems()
        {
        }

        /// <summary>
        /// Logs messages using the out-of-the-box ILogger from Microsoft.Xrm.Sdk.
        /// </summary>
        /// <param name="pluginLogger">ILogger instance from Microsoft.Xrm.Sdk.</param>
        /// <param name="level">Log level for ILogger.</param>
        /// <param name="message">Message to be logged.</param>
        /// <param name="traceParent">Trace parent for distributed tracing.</param>
        /// <returns>Task for logging.</returns>
        private async Task TraceWithILoggerAsync(Xrm.Sdk.PluginTelemetry.ILogger pluginLogger, Xrm.Sdk.PluginTelemetry.LogLevel level, string message, string traceParent)
        {
            var logMessage = string.IsNullOrEmpty(traceParent) ? message : $"{message} - TraceParent: {traceParent}";
            pluginLogger.Log(level, logMessage);
        }

        /// <summary>
        /// Logs messages using OpenTelemetry.
        /// </summary>
        /// <param name="logger">ILogger instance from Microsoft.Extensions.Logging.</param>
        /// <param="level">Log level for OpenTelemetry.</param>
        /// <param="message">Message to be logged.</param>
        /// <returns>Task for logging.</returns>
        private async Task LogOpenTelemetryAsync(ILogger logger, LogLevel level, string message)
        {
            logger.Log(level, message);
        }
    }
}
