param([switch]$Publish)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot)
try {
    dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    if ($Publish) {
        dotnet publish src/MCUGLauncher/MCUGLauncher.csproj -c Release -o artifacts/publish
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    }
} finally { Pop-Location }
