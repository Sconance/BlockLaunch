param([switch]$Publish)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot)
try {
    dotnet build src/MCUGLauncher/MCUGLauncher.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    if ($Publish) {
        dotnet publish src/MCUGLauncher/MCUGLauncher.csproj -c Release -o artifacts/publish
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
        dotnet publish src/MCUGLauncher/MCUGLauncher.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=false -o artifacts/publish-lite
        if ($LASTEXITCODE -ne 0) { throw 'Lite publish failed' }
        [xml]$project = Get-Content src/MCUGLauncher/MCUGLauncher.csproj
        $version = ($project.Project.PropertyGroup | Where-Object Version | Select-Object -First 1).Version
        New-Item artifacts/release -ItemType Directory -Force | Out-Null
        Copy-Item artifacts/publish/MCUGLauncher.exe "artifacts/release/MCUGLauncher-$version-Windows-x64.exe" -Force
        Copy-Item artifacts/publish-lite/MCUGLauncher.exe "artifacts/release/MCUGLauncher-$version-Windows-x64-Lite.exe" -Force
    }
} finally { Pop-Location }
