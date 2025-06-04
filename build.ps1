param(
    [string]$configuration = 'release',
    [bool]$skipInstallPackage = $false
)
    
if (-not $skipInstallPackage) {
    Write-Host "Installing BuildUtils module..."

    Install-package BuildUtils -Confirm:$false -Scope CurrentUser -Force
    Import-Module BuildUtils
}

$runningDirectory = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

dotnet tool restore
Assert-LastExecution -message "Unable to restore tooling." -haltExecution $true

$version = dotnet tool run dotnet-gitversion /config .config/GitVersion.yml | Out-String | ConvertFrom-Json
Write-host "GitVersion output: $version"

Write-Verbose "Parsed value to be returned"
$assemblyVer = $version.AssemblySemVer 
$assemblyFileVersion = $version.AssemblySemFileVer
$nugetPackageVersion = $version.NuGetVersionV2
$assemblyInformationalVersion = $version.FullBuildMetaData

Write-host "assemblyInformationalVersion   = $assemblyInformationalVersion"
Write-host "assemblyVer                    = $assemblyVer"
Write-host "assemblyFileVersion            = $assemblyFileVersion"
Write-host "nugetPackageVersion            = $nugetPackageVersion"

# Now restore packages and build everything.
Write-Host "\n\n*******************RESTORING PACKAGES*******************"
dotnet restore "$runningDirectory/src/Alkampfer.Assistant.sln"
Assert-LastExecution -message "Error in restoring packages." -haltExecution $true

Write-Host "\n\n*******************TESTING SOLUTION*******************"
dotnet test "src/Alkampfer.Assistant.Tests/Alkampfer.Assistant.Tests.csproj" `
    --collect:"XPlat Code Coverage" `
    --results-directory TestResults/ `
    --logger "trx;LogFileName=unittests.trx" `
    --no-restore `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

Assert-LastExecution -message "Error in test running." -haltExecution $true

Write-Host "\n\n*******************BUILDING SOLUTION*******************"
dotnet build "$runningDirectory/src/Alkampfer.Assistant.sln" --configuration release
Assert-LastExecution -message "Error in building in release configuration" -haltExecution $true
