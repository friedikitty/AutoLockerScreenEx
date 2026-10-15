# build.ps1 [-Configuration Debug|Release] [-Deploy [-DeployDir <path>]]
# Builds the solution. Without -Configuration, asks via a console menu.
# -Deploy always builds Release and copies the app files to -DeployDir
# (default D:\auto_locker_screen_ex): offers to kill a running instance there
# first, and to start it after copying.
param(
    [string]$Configuration = '',
    [switch]$Deploy,
    [string]$DeployDir = 'D:\auto_locker_screen_ex'
)

if ($Deploy) {
    if ($Configuration -eq 'Debug') { Write-Host 'Deploy always builds Release.' }
    $Configuration = 'Release'
}
if ($Configuration -notin 'Debug', 'Release') {
    $choices = [System.Management.Automation.Host.ChoiceDescription[]]@('&Debug', '&Release')
    $picked = $Host.UI.PromptForChoice('Build configuration', 'Select Debug or Release', $choices, 0)
    $Configuration = @('Debug', 'Release')[$picked]
}

dotnet build -c $Configuration (Join-Path $PSScriptRoot 'AutoLockerScreenEx.slnx')
if ($LASTEXITCODE -ne 0 -or -not $Deploy) { exit $LASTEXITCODE }

$binDir = Join-Path $PSScriptRoot 'AutoLockerScreenEx\bin\Release\net10.0-windows'
$targetExe = Join-Path $DeployDir 'AutoLockerScreenEx.exe'

$running = Get-Process -Name AutoLockerScreenEx -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $targetExe }
if ($running) {
    $choices = [System.Management.Automation.Host.ChoiceDescription[]]@('&Kill and continue', '&Abort deploy')
    $picked = $Host.UI.PromptForChoice('Deploy', "AutoLockerScreenEx is running from $DeployDir.", $choices, 0)
    if ($picked -ne 0) { Write-Host 'Deploy aborted.'; exit 1 }
    $running | Stop-Process -Force
    $running | Wait-Process -Timeout 5 -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Force $DeployDir | Out-Null
Get-ChildItem $binDir -Filter 'AutoLockerScreenEx.*' | Where-Object Extension -ne '.pdb' |
    Copy-Item -Destination $DeployDir
Write-Host "Deployed to $DeployDir"

$choices = [System.Management.Automation.Host.ChoiceDescription[]]@('&Start', '&Exit')
if ($Host.UI.PromptForChoice('Deploy', "Start AutoLockerScreenEx from $DeployDir?", $choices, 0) -eq 0) {
    Start-Process $targetExe
}
exit 0
