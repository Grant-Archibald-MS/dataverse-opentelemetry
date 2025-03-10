
using OpenTelemetry.Logs;
using OpenTelemetry;
using Microsoft.ApplicationInsights;

namespace Microsoft.Dataverse.Samples
{
    public class ApplicationInsightsProcessor : BaseProcessor<LogRecord>
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsProcessor(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public override void OnEnd(LogRecord record)
        {
            // Log the data to Application Insights
            _telemetryClient.TrackTrace(record.ToString());
        }
    }
}