using System;
using Microsoft.Dataverse.Samples;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;

public class ContextMapper {
    public DefaultValues GetDefaultValues(IPluginExecutionContext context, out string traceParent)
    {
        var defaults = new DefaultValues();

        switch (context.MessageName)
        {
            case "Create":
                defaults.Source = "EntityCreation";
                defaults.Stage = context.Stage == 20 ? "PreOperation" : "PostOperation";
                defaults.Message = $"{context.PrimaryEntityName} Entity has been created.";
                break;
            case "Update":
                defaults.Source = "EntityUpdate";
                defaults.Stage = context.Stage == 20 ? "PreOperation" : "PostOperation";
                defaults.Message = $"{context.PrimaryEntityName} Entity has been updated.";
                break;
            case "Delete":
                defaults.Source = "EntityDeletion";
                defaults.Stage = context.Stage == 20 ? "PreOperation" : "PostOperation";
                defaults.Message = $"{context.PrimaryEntityName} Entity has been deleted.";
                break;
            case "CustomApi":
                defaults.Source = "CustomApiExecution";
                defaults.Stage = context.Stage == 20 ? "PreOperation" : "PostOperation";
                defaults.Message = "Custom API has been executed.";
                break;
            default:
                defaults.Source = "UnknownOperation";
                defaults.Stage = "UnknownStage";
                defaults.Message = "Unknown operation.";
                break;
        }

        // Additional logic based on UserId
        if (context.UserId != Guid.Empty)
        {
            defaults.Message += $" by User {context.UserId}";
        }

        if ( context.InputParameters.ContainsKey("Source") ) {
            defaults.Source =  context.InputParameters["Source"] as string;
        }

        if ( context.InputParameters.ContainsKey("Stage") ) {
            defaults.Stage =  context.InputParameters["Stage"] as string;
        }

        var pluginLevel = LogLevel.Information;
        if ( context.InputParameters.ContainsKey("Level") ) {
            Enum.TryParse(context.InputParameters["Level"] as string, out pluginLevel);
        }

        if ( context.InputParameters.ContainsKey("Message") ) {
            defaults.Message =  context.InputParameters["Message"] as string;
        }

        defaults.Level = pluginLevel;
            
        traceParent = context.InputParameters.Contains("TraceParent") ? (string)context.InputParameters["TraceParent"] : string.Empty;

        // Check if the trace parent is in the shared variables
        if (string.IsNullOrEmpty(traceParent)) {
            if (context.SharedVariables.ContainsKey("tag") && !string.IsNullOrEmpty(context.SharedVariables["tag"] as string))
            {
                traceParent = context.SharedVariables["tag"] as string;
            }
        }

        return defaults;
    }
}