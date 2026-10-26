param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9]*Checks$')]
    [string[]]$Name,
    [string[]]$CheckArguments = @()
)

$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot)
try {
    foreach ($check in $Name) {
        $project = "test/$check/$check.csproj"
        if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
            throw "Check project not found: $project"
        }
    }
    foreach ($check in $Name) {
        Write-Host "Running $check"
        dotnet run --project "test/$check/$check.csproj" -c Release -- @CheckArguments
        if ($LASTEXITCODE -ne 0) { throw "$check failed with exit code $LASTEXITCODE" }
    }
} finally { Pop-Location }
