[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [string] $OutputPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$packageProject = Join-Path $repositoryRoot 'src\Blazorade.StaticPages\Blazorade.StaticPages.csproj'
$generatorHostProject = Join-Path $repositoryRoot 'src\Blazorade.StaticPages.Generator.Host\Blazorade.StaticPages.Generator.Host.csproj'
$packageProjectDirectory = Split-Path -Parent $packageProject
$generatorHostDirectory = Split-Path -Parent $generatorHostProject

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repositoryRoot 'artifacts\packages'
}
else {
    $OutputPath = [System.IO.Path]::GetFullPath($OutputPath, $repositoryRoot)
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    Write-Host "dotnet $($Arguments -join ' ')" -ForegroundColor DarkGray
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet exited with code $LASTEXITCODE."
    }
}

function Get-RequiredFileHash {
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Expected file was not produced: $Path"
    }

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

Write-Host "Building Blazorade.StaticPages package ($Configuration)..." -ForegroundColor Cyan

$projectFilesToClean = @(
    $packageProjectDirectory,
    $generatorHostDirectory,
    (Join-Path $repositoryRoot 'src\Blazorade.StaticPages.Generator')
)

foreach ($directory in $projectFilesToClean) {
    foreach ($outputDirectory in @('bin', 'obj')) {
        $path = Join-Path $directory $outputDirectory
        if (Test-Path -LiteralPath $path) {
            Write-Host "Removing $path" -ForegroundColor DarkGray
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

if (Test-Path -LiteralPath $OutputPath) {
    Get-ChildItem -LiteralPath $OutputPath -Filter '*.nupkg' -File -ErrorAction SilentlyContinue |
        Remove-Item -Force
}
else {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$buildProperties = @(
    "Configuration=$Configuration",
    'GeneratePackageOnBuild=false',
    'RestoreIgnoreFailedSources=true'
)

# Build the generator first so its output is fresh before the package project
# copies the generator host files into the NuGet package.
Invoke-DotNet @('build', $generatorHostProject, '--configuration', $Configuration, '--no-incremental', "-p:$($buildProperties[0])", "-p:$($buildProperties[1])", "-p:$($buildProperties[2])")
Invoke-DotNet @('build', $packageProject, '--configuration', $Configuration, '--no-incremental', "-p:$($buildProperties[0])", "-p:$($buildProperties[1])", "-p:$($buildProperties[2])")

$packageProperties = @(
    "PackageOutputPath=$OutputPath",
    'GeneratePackageOnBuild=false',
    'RestoreIgnoreFailedSources=true'
)

Invoke-DotNet @('pack', $packageProject, '--configuration', $Configuration, '--no-build', "-p:$($packageProperties[0])", "-p:$($packageProperties[1])", "-p:$($packageProperties[2])")

$packageVersion = [System.Xml.Linq.XDocument]::Load($packageProject).Root.Element('PropertyGroup').Element('Version').Value
$packagePath = Join-Path $OutputPath "Blazorade.StaticPages.$packageVersion.nupkg"

if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
    throw "The expected package was not produced: $packagePath"
}

$generatorHostOutput = Join-Path $generatorHostDirectory "bin\$Configuration\net10.0"
$runtimeAssembly = Join-Path $packageProjectDirectory "bin\$Configuration\net10.0\Blazorade.StaticPages.dll"
$generatorAssembly = Join-Path $generatorHostOutput 'Blazorade.StaticPages.Generator.dll'
$generatorHostExecutable = Join-Path $generatorHostOutput 'Blazorade.StaticPages.Generator.Host.dll'

$temporaryPackageDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "Blazorade.StaticPages-package-$([guid]::NewGuid())"
try {
    Expand-Archive -LiteralPath $packagePath -DestinationPath $temporaryPackageDirectory

    $packageFiles = @{
        'lib/net10.0/Blazorade.StaticPages.dll' = $runtimeAssembly
        'tools/net10.0/Blazorade.StaticPages.Generator.dll' = $generatorAssembly
        'tools/net10.0/Blazorade.StaticPages.Generator.Host.dll' = $generatorHostExecutable
    }

    foreach ($entry in $packageFiles.GetEnumerator()) {
        $packagedFile = Join-Path $temporaryPackageDirectory ($entry.Key -replace '/', '\')
        $expectedHash = Get-RequiredFileHash $entry.Value
        $actualHash = Get-RequiredFileHash $packagedFile
        if ($expectedHash -ne $actualHash) {
            throw "Package verification failed for '$($entry.Key)'. The packaged file does not match the freshly built output."
        }
    }
}
finally {
    if (Test-Path -LiteralPath $temporaryPackageDirectory) {
        Remove-Item -LiteralPath $temporaryPackageDirectory -Recurse -Force
    }
}

Write-Host "Package created and verified:" -ForegroundColor Green
Write-Host "  $packagePath"
