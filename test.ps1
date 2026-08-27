# test.ps1 [-Configuration Debug|Release]
# Runs CLI smoke tests against the built exe (read-only; never changes settings).
# Without -Configuration, asks via a console menu. Exit code 0 = all passed.
param([string]$Configuration = '')

if ($Configuration -notin 'Debug', 'Release') {
    $choices = [System.Management.Automation.Host.ChoiceDescription[]]@('&Debug', '&Release')
    $picked = $Host.UI.PromptForChoice('Test configuration', 'Select which build to test', $choices, 0)
    $Configuration = @('Debug', 'Release')[$picked]
}

$exe = Join-Path $PSScriptRoot "AutoLockerScreenEx\bin\$Configuration\net10.0-windows\AutoLockerScreenEx.exe"
if (-not (Test-Path $exe)) {
    & (Join-Path $PSScriptRoot 'build.ps1') -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$script:failed = 0
function Check([string]$name, [bool]$cond) {
    if ($cond) { "PASS  $name" } else { "FAIL  $name"; $script:failed++ }
}

$out = & $exe check_lock
Check 'check_lock exit 0'      ($LASTEXITCODE -eq 0)
Check 'check_lock prints json' ($out -match '^\{"ac":\d+,"dc":\d+\}$')

& $exe -h | Out-Null
Check '-h exit 0'              ($LASTEXITCODE -eq 0)

& $exe /? | Out-Null
Check '/? exit 0'              ($LASTEXITCODE -eq 0)

& $exe bogus 2>$null | Out-Null
Check 'bad arg exit 2'         ($LASTEXITCODE -eq 2)

& $exe set_lock 2>$null | Out-Null
Check 'set_lock w/o arg exit 2' ($LASTEXITCODE -eq 2)

if ($script:failed -gt 0) { "$script:failed test(s) failed"; exit 1 }
'all tests passed'
