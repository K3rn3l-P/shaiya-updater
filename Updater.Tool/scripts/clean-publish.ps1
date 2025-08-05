# PowerShell script to clean Updater.Tool publish directory
# Keeps only the essential files for Updater.Tool

param([string]$publishDir)

# Rimuovi eventuali virgolette finali/spazi dal parametro
$publishDir = $publishDir.Trim('"').Trim()

if (!(Test-Path $publishDir)) {
    Write-Host "Publish directory does not exist: $publishDir"
    exit 0
}

$keep = @(
    "Duff.Updater.dll",
    "Updater.dll",
    "Updater.Tool.deps.json",
    "Updater.Tool.dll",
    "Updater.Tool.exe",
    "Updater.Tool.runtimeconfig.json"
)

# Remove all files not in the keep list
Get-ChildItem -Path $publishDir -File | Where-Object { $_.Name -notin $keep } | Remove-Item -Force
# Remove all directories (e.g. language folders)
Get-ChildItem -Path $publishDir -Directory | Remove-Item -Recurse -Force
