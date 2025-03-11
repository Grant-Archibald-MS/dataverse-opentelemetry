$config = (Get-Content -Path $PSScriptRoot\config.json -Raw | ConvertFrom-Json)

$environmentUrl = $config.environmentUrl
$customApiName = $config.customApiName

$token = (az account get-access-token --resource=$environmentUrl --query accessToken --output tsv)

$testUrl = "${environmentUrl}api/data/v9.0/${customApiName}" #?tag=00-9408ba64a337d07440b7efbf263b7bae-815572cbfd7088e2-00"

$body = @{
    Source = "Sample"
    Stage = "1"
    Level = "Information"
    Message = "2"
#    TraceParent = "00-9408ba64a337d07440b7efbf263b7bae-00b5ae3c5cecdbdd-00"
}

$jsonBody = $body | ConvertTo-Json

$response = Invoke-RestMethod -Uri $testUrl -Method "POST" -Headers @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
} -Body $jsonBody


$response | ConvertTo-Json -Depth 10