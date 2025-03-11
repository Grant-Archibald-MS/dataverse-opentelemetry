# Overview
The ApplicationInsightsPlugin is designed to enhance telemetry capabilities within a Dataverse environment by leveraging Application Insights. This plugin expects to be registered as a Custom API, which defines a Message. The Message is configured to allow both synchronous and asynchronous actions. Once the message is created, an additional step can be added to the plugin for the created message type to call Observability.

## W3C TraceParent
The W3C Trace Context standard defines a format for propagating trace information across different systems and components. The TraceParent is a key part of this standard, allowing end-to-end tracing of transactions across multiple components. When a TraceParent is supplied, it creates a dependency record, enabling detailed tracking and correlation of telemetry data. For more information on the [W3C Trace Context standard](https://www.w3.org/TR/trace-context/)

## Build

Use the following commands to build the plugin 

1. Confirm that you have .Net 4.6.2 installed

```pwsh
Get-ChildItem "hklm:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\" | Get-ItemPropertyValue -Name Release | ForEach-Object {
    switch ($_ -ge 394802) {
        $true  { Write-Host "Confirmed that .Net 4.6.2 is installed!" }
        $false { Write-Host ".Net 4.6.2 is NOT installed! Aborting Update!"}
    }
}
```

2. Change to the project directory

```
cd src\plugin
```

3. Compile the project

```pwsh
dotnet build --configuration Release
```

## Registration

Once you have the plugin compiled follow these steps to create a message 

1. Install the Power Platform CLI (pac) if you haven't already:

```pwsh
dotnet tool install -g Microsoft.PowerPlatform.Cds.Client
```

2. Open the Plugin Registration Tool:

```pwsh
pac tool prt
```

3. Connect to your Dataverse environment using the Plugin Registration Tool.

4. Register the plugin step:

- In the Plugin Registration Tool, import the package.
- Select the imported package from bin\Release folder

5. Select **Register**

6. Select **Register New Custom API**

7. Enter the **Display Name**

8. Select a **Solution**.

   > NOTE: You **DO NOT** need to add an Assembly or Plugin for the Custom API registration

9. Select **Sync and Async** option for **Allow Custom Process Step Type**

10. Add Request Parameters as string **Source**, **Stage**, **Level**, **Message**

11. Add Request Parameter **TraceParent** as string that is optional

12. Add Response Parameter **TraceParent** as string

13. Select **Register**

14. Select **Refresh** to ensure the new message is loaded

## Post Actions

After you have defined the message to associate the plugin with the registered message

1. Select **View**

2. Select **Display by Package**

3. Select your plugin to want to register for example **ApplicationInsightsPlugin**

4. Select **Register New Step**

5. Select you message. For example **cat_Telemetry**

6. Select **PostOperation** for Event Pipeline Stage of Execution.

7. Add the json for the Unsecure Configuration

```json
{
    "Level":"Information",
    "Enabled":true
}
```

  > NOTES:
  > 1. You can use Level to control the level of information written
  > 2. Enabled will control if data is written to Application Insights

8. Add the Connection string for Application Insights to Secure Configuration

9. Select **Register New Step**

## Extending the example with Entity messages

You can also extend the plugin to apply to dataverse entities using the following example

1. In the plugin registration tool select the plugin

2. Select **Register New Step**

3. Select message type **Create**

4. Select primary entity. For example **account**

5. Choose when run for example **PostOperation** 

6. Choose execution mode. For example **Asynchronous**

7. Enter a unsecure configuration with Level of Information, Enabled and Output field

```json
{
    "Level":"Information",
    "Enabled":true,
    "OutputField": "Message"
}
```

## Verify

To verify the plugin

1. Create config file with correct environment and API Name imported and optional entity to test

```json
{
    "environmentUrl": "https://contoso.crm.dynamics.com/",
    "customApiName": "contoso_Telemetry",
    "entityName": "accounts"
}
```

2. Ensure logged out of Azure CLI

```
az logout
```

3. Login to Azure CLI with user account or Service Principal that has rights to run the Plugin

```
az login --use-device-code --allow-no-subscriptions
```

4. Execute the plugin

```
./run.ps1
```

5. Lookup in Application Insights by the operation id

