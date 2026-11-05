param([string]$OutputDirectory = 'comparison-rerun')
$ErrorActionPreference = 'Stop'
$taskRepo = $PSScriptRoot
$taskProject = Join-Path $taskRepo 'Blockchain/Blockchain.csproj'
$taskData = Join-Path $taskRepo 'Blockchain/test-data/konstitucija.txt'
if (-not (Test-Path -LiteralPath $taskData)) { throw 'Trūksta Blockchain/test-data/konstitucija.txt' }
$taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRepo $OutputDirectory))
if (Test-Path -LiteralPath $taskOutput) { throw 'Rezultatų aplankas jau yra. Nurodyk naują -OutputDirectory, kad išsaugotum ankstesnius matavimus.' }
& dotnet build $taskProject -c Release
if ($LASTEXITCODE -ne 0) { throw 'Nepavyko sukompiliuoti' }
$taskDll = Join-Path $taskRepo 'Blockchain/bin/Release/net9.0/Blockchain.dll'
$taskSalt = Join-Path $taskRepo 'comparison/v0.1/results/guessing-salt.hex'
foreach ($version in @('v0.1','v0.2')) {
    $runPath = Join-Path $taskOutput $version
    New-Item -ItemType Directory -Force -Path "$runPath/test-data", "$runPath/results" | Out-Null
    Copy-Item -LiteralPath $taskData -Destination "$runPath/test-data/konstitucija.txt"
    if (Test-Path -LiteralPath $taskSalt) {
        Copy-Item -LiteralPath $taskSalt -Destination "$runPath/results/guessing-salt.hex"
    }
    Push-Location $runPath
    try {
        foreach ($flag in @('--test','--test','--avalanche','--collisions','--performance','--guessing')) {
            & dotnet $taskDll --version $version $flag >> "$($flag.TrimStart('-')).log"
            if ($LASTEXITCODE -ne 0) { throw "$version $flag FAIL" }
            Write-Output "$version $flag PASS"
        }
    } finally { Pop-Location }
    if ($version -eq 'v0.1') { $taskSalt = Join-Path $runPath 'results/guessing-salt.hex' }
}
Write-Output "Rezultatai: $taskOutput. Grafikai: python Generate-Graphs.py `"$taskOutput`""
