param(
    [string]$constantsPath,
    [string]$csprojPath
)

if (!(Test-Path $constantsPath)) {
    Write-Error "Constants.cs not found: $constantsPath"
    exit 1
}
if (!(Test-Path $csprojPath)) {
    Write-Error "csproj not found: $csprojPath"
    exit 1
}

$constantsContent = Get-Content $constantsPath -Raw
if ($constantsContent -match 'public const int UpdaterVersion = (\d+);') {
    $version = $matches[1]
    $verString = "3.8.1.$version"

    # Backup
    Copy-Item $csprojPath ($csprojPath + ".bak") -Force

    $lines = Get-Content $csprojPath
    $lines = $lines | ForEach-Object {
        if ($_ -match '<AssemblyVersion>\d+\.\d+\.\d+\.\d+</AssemblyVersion>') {
            $_ -replace '<AssemblyVersion>\d+\.\d+\.\d+\.\d+</AssemblyVersion>', "<AssemblyVersion>$verString</AssemblyVersion>"
        } elseif ($_ -match '<FileVersion>\d+\.\d+\.\d+\.\d+</FileVersion>') {
            $_ -replace '<FileVersion>\d+\.\d+\.\d+\.\d+</FileVersion>', "<FileVersion>$verString</FileVersion>"
        } else {
            $_
        }
    }
    Set-Content $csprojPath $lines -Encoding UTF8
} else {
    Write-Error "UpdaterVersion not found in Constants.cs"
    exit 1
}