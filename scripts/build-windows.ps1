param(
    [ValidateSet('Legacy', 'Front', 'Both')]
    [string]$Frontend = 'Both',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') {
    throw 'This script requires Windows x64 and the .NET SDK specified in global.json.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found. Install the .NET SDK specified in global.json and reopen PowerShell.'
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$targets = @(
    @{ Name = 'Legacy'; Directory = 'legacy'; Project = 'src/MCUGLauncher/MCUGLauncher.csproj'; Executable = 'MCUGLauncher.exe' },
    @{ Name = 'Front'; Directory = 'front'; Project = 'src/MCUGLauncher/Front/Front.csproj'; Executable = 'MCUGLauncher.Front.exe' }
)
$command = if ($Publish) { 'publish' } else { 'build' }

Push-Location $repositoryRoot
try {
    dotnet --version
    if ($LASTEXITCODE -ne 0) {
        throw 'The SDK required by global.json is unavailable. Install it before building.'
    }
    foreach ($target in $targets) {
        if ($Frontend -ne 'Both' -and $Frontend -ne $target.Name) {
            continue
        }
        $output = Join-Path $repositoryRoot "artifacts/windows/$($target.Directory)/$Configuration/$command"
        Write-Host "[$($target.Name)] $command -> $output"
        dotnet $command $target.Project -c $Configuration -r win-x64 --self-contained true -o $output
        if ($LASTEXITCODE -ne 0) {
            throw "$($target.Name) $command failed with exit code $LASTEXITCODE."
        }
        $executable = Join-Path $output $target.Executable
        if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
            throw "Expected executable was not produced: $executable"
        }
        if ($target.Name -eq 'Front') {
            Write-Host 'Front uses real settings, accounts, instances and downloads. Close the legacy launcher before using the same configuration.'
        }
        Write-Host "READY: $executable"
    }
} finally {
    Pop-Location
}
