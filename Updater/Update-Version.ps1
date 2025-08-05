param(
    [string]$ConstantsFile = "Common\Constants.cs",
    [string]$CsprojFile = "Updater.csproj"
)

# Calcola la versione: yy.MM.dd.HH e ddMMyyHH
$now = Get-Date
$yy = $now.ToString("yy")
$MM = $now.ToString("MM")
$dd = $now.ToString("dd")
$HH = $now.ToString("HH")
$version = "$yy.$MM.$dd.$HH"
$ddmmyyhh = "$dd$MM$yy$HH"

# Fai una copia di backup del file csproj
Copy-Item $CsprojFile ($CsprojFile + ".bak") -Force

# Aggiorna Updater.csproj (AssemblyVersion e FileVersion)
(Get-Content $CsprojFile) | ForEach-Object {
    if ($_ -match '<AssemblyVersion>') {
        "    <AssemblyVersion>$version</AssemblyVersion>"
    } elseif ($_ -match '<FileVersion>') {
        "    <FileVersion>$version</FileVersion>"
    } else {
        $_
    }
} | Set-Content $CsprojFile

# Aggiorna la costante UpdaterVersion in Constants.cs con ddMMyyHH
(Get-Content $ConstantsFile) | ForEach-Object {
    if ($_ -match 'public const int UpdaterVersion =') {
        "        public const int UpdaterVersion = $ddmmyyhh;"
    } else {
        $_
    }
} | Set-Content $ConstantsFile

Write-Host "Updated $CsprojFile to $version and $ConstantsFile UpdaterVersion to $ddmmyyhh"