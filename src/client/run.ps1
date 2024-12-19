
dotnet build

$location = Get-location

Set-Location bin\Debug

dotnet run client.dll -


Set-Location $location