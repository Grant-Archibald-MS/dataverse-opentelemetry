 namespace Microsoft.Dataverse.Samples
{
    /// <summary>
    /// Configuration class for Application Insights.
    /// </summary>
    public class ApplicationInsightsConfig
    {
        public bool Enabled { get; set; }
        public string LogLevel { get; set; }
        public string OutputField { get; set; } = "TraceParent";
        public bool Append { get; set; } = false;
    }
}