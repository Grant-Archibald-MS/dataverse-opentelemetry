To run this sample you can execute the following:

1. Run the program

```pwsh
dotnet run
```

2. Enter the URL of the environment you wish to insert the Account into

3. Enter the OAUTH token. You can copy from the AccessToken of the following

```pwsh
az login -allow-no-subscriptions
az account get-access-token --resource https://yourorg.crm.dynamics.com/
```