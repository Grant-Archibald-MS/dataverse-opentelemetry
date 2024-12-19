Use the following steps to register the plugin taht will use the tag to update the requested account name

1. Build the plugin

```pwsh
dotnet build --configuration Release
```

2. Install the Power Platform CLI (pac) if you haven't already:

```pwsh
dotnet tool install -g Microsoft.PowerPlatform.Cds.Client
```

3. Open the Plugin Registration Tool:

```pwsh
pac tool prt
```

4. Connect to your Dataverse environment using the Plugin Registration Tool.

5. Register the plugin step:

- In the Plugin Registration Tool, import the package.
- Select the imported assembly
- Click on "Register" and select "Register New Step".
- Configure the step with the following details:
  - Message: Create
  - Primary Entity: account
  - Event Pipeline Stage of Execution: Pre-operation.
