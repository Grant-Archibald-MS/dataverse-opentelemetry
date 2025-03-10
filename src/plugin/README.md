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

  > IMPORTANT: Versions of OpenTelemetry **greater than 1.8.0** have compatibility issues with being imported into Dataverse s Plugin solution

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

3. Select your plugin

4. Select **Register**

5. Select you message. For example **cat_OpenTelementry**

6. Select **PostOperation** for Event Pipeline Stage of Execution.

7. Add the json for the Unsecure Configuration

```json
{
    "Level":"Information",
    "EnableOpenTelemetry":true
}
```

8. Add the Connection string for Application Insights to Secure Configuration

9. Select **Register New Step**

## Verify

To verify the plugin

1. Create config file with correct environment and API Name imported as

```json
{
    "environmentUrl": "https://contoso.crm.dynamics.com/",
    "customApiName": "contoso_OpenTelemetry"
}
```

2. Ensure logged out of Azure CLI

```
az logout
```

3. Login to Azure CLI with user account or Sevice Principal that has rights to run the Plugin

```
az login --use-device-code --allow-no-subscriptions
```

4. Execute the plugin

```
./run.ps1
```
