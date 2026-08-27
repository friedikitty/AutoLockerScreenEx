# run.ps1 [-Configuration Debug|Release]
# Launches the app (builds first if the exe is missing).
# Without -Configuration, asks via a console menu.
param([string]$Configuration = '')

if ($Configuration -notin 'Debug', 'Release') {
    $choices = [System.Management.Automation.Host.ChoiceDescription[]]@('&Debug', '&Release')
    $picked = $Host.UI.PromptForChoice('Run configuration', 'Select which build to run', $choices, 0)
    $Configuration = @('Debug', 'Release')[$picked]
}

$exe = Join-Path $PSScriptRoot "AutoLockerScreenEx\bin\$Configuration\net10.0-windows\AutoLockerScreenEx.exe"
if (-not (Test-Path $exe)) {
    & (Join-Path $PSScriptRoot 'build.ps1') -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Start-Process $exe
