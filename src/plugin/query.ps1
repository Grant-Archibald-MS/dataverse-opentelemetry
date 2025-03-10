$config = (Get-Content -Path $PSScriptRoot\config.json -Raw | ConvertFrom-Json)

$environmentUrl = $config.environmentUrl
$customApiName = $config.customApiName

$token = (az account get-access-token --resource=$environmentUrl --query accessToken --output tsv)

$url = "${environmentUrl}api/data/v9.2/sdkmessages?`$filter=name%20eq%20%27${customApiName}%27"

$response = Invoke-RestMethod -Uri $url -Headers @{Authorization = "Bearer $token"}

$messageId = $response.value.sdkmessageid
$getStepMessage ="${environmentUrl}api/data/v9.2/sdkmessageprocessingsteps?`$filter=_sdkmessageid_value%20eq%20%27${messageId}%27"

$stepResponse = (Invoke-RestMethod -Uri $getStepMessage -Headers @{Authorization = "Bearer $token"})

if ($stepResponse.value.Count -eq 0) {
    Write-Error "No SDK message processing steps found for the given message ID."
    return
}

$stepId = $stepResponse.value[0].sdkmessageprocessingstepid

$stepPatch ="${environmentUrl}api/data/v9.2/sdkmessageprocessingsteps(${stepId})"

# $update = @{
#     configuration = "{Level:'Infomation',EnableOpenTelemetry:true}"
# } | ConvertTo-Json

# # Make the PATCH request
# $response = Invoke-RestMethod -Uri $stepPatch -Method Patch -Headers @{
#     Authorization = "Bearer $token"
#     "Content-Type" = "application/json"
# } -Body $update

# return

# $secureUrl = "${environmentUrl}api/data/v9.2/sdkmessageprocessingstepsecureconfigs?`$filter=sdkmessageprocessingstepsecureconfigidunique%20eq%20%27${stepId}%27"

# $secure = (Invoke-RestMethod -Uri $secureUrl -Headers @{Authorization = "Bearer $token"})

# $secure

# # Output the response
# $response

