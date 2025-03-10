using Microsoft.Extensions.Logging;

namespace Microsoft.Dataverse.Samples
{
    /// <summary>
    /// Configuration class for OpenTelemetry in the plugin.
    /// </summary>
    internal class OpenTelemetryConfig
    {
        public string LogLevel { get; set; } = "Information";
        public bool EnableOpenTelemetry { get; set; } = false;
    }
}
