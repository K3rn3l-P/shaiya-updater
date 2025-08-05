param(
    [string]$AssemblyInfo = "src\AssemblyInfo.cpp",
    [string]$ResourceRc = "resources\Resource.rc"
)

# Calcola la versione: yy.MM.dd.HH
$now = Get-Date
$yy = $now.ToString("yy")
$MM = $now.ToString("MM")
$dd = $now.ToString("dd")
$HH = $now.ToString("HH")
$version = "$yy.$MM.$dd.$HH"
$versionCommas = "$yy, $MM, $dd, $HH"

# Backup AssemblyInfo.cpp
if (Test-Path $AssemblyInfo) {
    Copy-Item $AssemblyInfo "$AssemblyInfo.bak" -Force
}

# Aggiorna AssemblyInfo.cpp in modo sicuro
$lines = Get-Content $AssemblyInfo
$found = $false
$updated = $lines | ForEach-Object {
    if ($_ -match 'AssemblyVersionAttribute') {
        $found = $true
        "[assembly:AssemblyVersionAttribute(L`"$version`")];"
    } else {
        $_
    }
}
if (-not $found) {
    $updated += "[assembly:AssemblyVersionAttribute(L`"$version`")];"
}
$updated | Set-Content $AssemblyInfo

# Backup Resource.rc
if (Test-Path $ResourceRc) {
    Copy-Item $ResourceRc "$ResourceRc.bak" -Force
}

# Aggiorna Resource.rc (match robusto su spazi)
(Get-Content $ResourceRc) | ForEach-Object {
    if ($_ -match '^\s*FILEVERSION') {
        " FILEVERSION $versionCommas"
    } elseif ($_ -match '^\s*PRODUCTVERSION') {
        " PRODUCTVERSION $versionCommas"
    } elseif ($_ -match 'VALUE \"FileVersion\"') {
        "VALUE `"FileVersion`", `"$version`""
    } elseif ($_ -match 'VALUE \"ProductVersion\"') {
        "VALUE `"ProductVersion`", `"$version`""
    } else {
        $_
    }
} | Set-Content $ResourceRc

Write-Host "Updated AssemblyInfo.cpp and Resource.rc to $version"