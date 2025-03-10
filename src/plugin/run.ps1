$config = (Get-Content -Path $PSScriptRoot\config.json -Raw | ConvertFrom-Json)

$environmentUrl = $config.environmentUrl
$customApiName = $config.customApiName

$token = (az account get-access-token --resource=$environmentUrl --query accessToken --output tsv)

$testUrl = "${environmentUrl}api/data/v9.0/${customApiName}" #?tag=00-2d708f92ccca7d80ca0ba94bfc97f32a-df0cb752131c1561-01"

$body = @{
    Source = "Sample"
    Stage = "1"
    Level = "Debug"
    Message = "2"
    # TraceParent = "00-2d708f92ccca7d80ca0ba94bfc97f32a-df0cb752131c1561-01"
}

$jsonBody = $body | ConvertTo-Json

$response = Invoke-RestMethod -Uri $testUrl -Method "POST" -Headers @{
    Authorization = "Bearer $token"
    "Content-Type" = "application/json"
} -Body $jsonBody


$response | ConvertTo-Json -Depth 10