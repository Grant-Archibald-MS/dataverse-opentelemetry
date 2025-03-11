$config = (Get-Content -Path $PSScriptRoot\config.json -Raw | ConvertFrom-Json)

$environmentUrl = $config.environmentUrl
$customApiName = $config.customApiName
$entityName = $config.entityName

$token = (az account get-access-token --resource=$environmentUrl --query accessToken --output tsv)

if ( -not [string]::IsNullOrEmpty($customApiName) ) {
    Write-Host "Custom API Name: $customApiName"
    
    # Start recording information
    $testUrl = "${environmentUrl}api/data/v9.0/${customApiName}" 

    $body = @{
        Source = "Sample"
        Stage = "1"
        Level = "Information"
        Message = "Some data"
    }

    $jsonBody = $body | ConvertTo-Json

    $response = Invoke-RestMethod -Uri $testUrl -Method "POST" -Headers @{
        Authorization = "Bearer $token"
        "Content-Type" = "application/json"
    } -Body $jsonBody

    $traceParent = $response.TraceParent

    Write-Host "TraceParent: $traceParent"

    # Add dependent data using tag
    $testUrl = "${environmentUrl}api/data/v9.0/${customApiName}?tag=${traceParent}" 

    $body = @{
        Source = "Sample"
        Stage = "2"
        Level = "Information"
        Message = "Some more data"
    }
    $jsonBody = $body | ConvertTo-Json

    $childResponse = Invoke-RestMethod -Uri $testUrl -Method "POST" -Headers @{
        Authorization = "Bearer $token"
        "Content-Type" = "application/json"
    } -Body $jsonBody

    $child = $childResponse.TraceParent

    Write-Host "TraceParent (Child): $child"

    # Add dependent data using message
    $testUrl = "${environmentUrl}api/data/v9.0/${customApiName}" 

    $body = @{
        Source = "Sample"
        Stage = "3"
        Level = "Information"
        Message = "Some further data"
        TraceParent = $child
    }
    $jsonBody = $body | ConvertTo-Json

    $grandChildResponse = Invoke-RestMethod -Uri $testUrl -Method "POST" -Headers @{
        Authorization = "Bearer $token"
        "Content-Type" = "application/json"
    } -Body $jsonBody

    $grandchild = $grandChildResponse.TraceParent

    Write-Host "TraceParent (Grandchild): $grandchild"
}

if ( -not [string]::IsNullOrEmpty($entityName) ) {
    Write-Host "Entity Name: $entityName"
    
    # Create the entity and include it as part of the trace
    $testUrl = "${environmentUrl}api/data/v9.0/${entityName}?tag=${traceParent}" 

    $body = @{
        name = "Test ${entityName}"
        description = "Sample data"
    }

    $jsonBody = $body | ConvertTo-Json

    Write-Host "Creating entity: $testUrl"

    $response = Invoke-RestMethod -Uri $testUrl -Method "POST" -Headers @{
        Authorization = "Bearer $token"
        "Content-Type" = "application/json"
    } -Body $jsonBody

    Write-Host $response.id
}