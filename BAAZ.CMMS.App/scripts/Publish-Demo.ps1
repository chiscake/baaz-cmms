# Builds an unpackaged self-contained distribution folder.
param(
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64",
    [switch]$Zip
)

$ErrorActionPreference = "Stop"
$ProjectDir = Resolve-Path (Join-Path $PSScriptRoot "..")
$ProjectName = Split-Path $ProjectDir -Leaf
$DemoProject = Join-Path $ProjectDir "$ProjectName.csproj"
$PublishProfile = switch ($Platform) {
    "x86" { "win-x86" }
    "ARM64" { "win-arm64" }
    default { "win-x64" }
}

if (-not (Test-Path -LiteralPath $DemoProject)) {
    throw "Project file not found: $DemoProject"
}

Write-Host "Publishing $ProjectName ($PublishProfile)..."
dotnet publish $DemoProject -c Release -p:Platform=$Platform /p:PublishProfile=$PublishProfile
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$publishDir = Get-ChildItem -Path (Join-Path $ProjectDir "bin\Release") -Directory -Recurse -Filter "publish" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $publishDir) {
    Write-Warning "Publish succeeded but publish folder was not found under bin\Release"
    exit 0
}

Write-Host ""
Write-Host "Distribution folder (zip and share):"
Write-Host "  $($publishDir.FullName)"
Write-Host ""
Write-Host "Run on target PC:"
Write-Host "  $(Join-Path $publishDir.FullName "$ProjectName.exe")"

if ($Zip) {
    $distDir = Join-Path $ProjectDir "dist"
    New-Item -ItemType Directory -Force -Path $distDir | Out-Null
    $zipPath = Join-Path $distDir "$ProjectName-$Platform.zip"
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    Compress-Archive -Path (Join-Path $publishDir.FullName "*") -DestinationPath $zipPath
    Write-Host ""
    Write-Host "Zip archive:"
    Write-Host "  $zipPath"
}
