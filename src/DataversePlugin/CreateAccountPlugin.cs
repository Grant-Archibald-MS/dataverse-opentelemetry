using Microsoft.Xrm.Sdk;
using System;

namespace DataversePlugin
{
    /// <summary>
    /// Plugin development guide: https://docs.microsoft.com/powerapps/developer/common-data-service/plug-ins
    /// Best practices and guidance: https://docs.microsoft.com/powerapps/developer/common-data-service/best-practices/business-logic/
    /// </summary>
    public class CreateAccountPlugin : PluginBase
    {
        public CreateAccountPlugin(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(CreateAccountPlugin))
        {
            // TODO: Implement your custom configuration handling
            // https://docs.microsoft.com/powerapps/developer/common-data-service/register-plug-in#set-configuration-data
        }

        // Entry point for custom business logic execution
        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;

            // Check if the input parameters contain the target entity.
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.
                Entity entity = (Entity)context.InputParameters["Target"];

                // Check if the entity is an account.
                if (entity.LogicalName == "account")
                {
                    // Check if the tag parameter exists in SharedVariables.
                    if (context.SharedVariables.ContainsKey("tag"))
                    {
                        string tagValue = context.SharedVariables["tag"].ToString();

                        // Append the tag to the account name.
                        if (entity.Attributes.Contains("name"))
                        {
                            entity["name"] = $"{entity["name"]} [{tagValue}]";
                        }
                    }
                }
            }
        }
    }
}
