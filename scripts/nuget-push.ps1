$ErrorActionPreference = 'Stop'

$rootPath = 'C:\Users\elkha\RiderProjects\portfolio project'
$packages = @(
  Join-Path $rootPath 'services\company-api\CompanyApi.Contracts'
  Join-Path $rootPath 'services\job-api\JobApi.Contracts'
  Join-Path $rootPath 'services\user-api\UserAPI.Contracts'
  Join-Path $rootPath 'services\Elkhair.Dev.Common'
  Join-Path $rootPath 'services\HealthChecks\JobBoard.HealthChecks'
)

$sourceUrl = 'https://nuget.eelkhair.net/v3/index.json'
$apiKey    = $env:NUGET_API_KEY
if (-not $apiKey) { $apiKey = 'MySecretApi' }  # replace or set env var

foreach ($dir in $packages) {
  Write-Host "Packing: $dir"
  Set-Location $dir

  $nugetDir = Join-Path $dir 'nuget'
  if (Test-Path $nugetDir) { Remove-Item $nugetDir -Recurse -Force }
  New-Item $nugetDir -ItemType Directory | Out-Null

  dotnet pack -c Release -o $nugetDir

  Get-ChildItem -Path (Join-Path $nugetDir '*.nupkg') | ForEach-Object {
    Write-Host "Pushing: $($_.FullName)"
    dotnet nuget push $_.FullName --source $sourceUrl --api-key $apiKey --skip-duplicate
  }

  Remove-Item $nugetDir -Recurse -Force
}