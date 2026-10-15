# build.ps1 [-Configuration Debug|Release] [-Deploy [-DeployDir <path>]]
# Builds the solution; asks via console menus for anything not passed:
# Debug/Release, then deploy y/n (Enter = no) and deploy dir (Enter = default).
# Deploy copies the built app files to DeployDir (default D:\auto_locker_screen_ex):
# offers to kill a running instance there first, and to start it after copying.
param(
    [string]$Configuration = '',
    [switch]$Deploy,
    [string]$DeployDir = 'D:\auto_locker_screen_ex'
)

if ($Configuration -notin 'Debug', 'Release') {
    $choices = [System.Management.Automation.Host.ChoiceDescription[]]@('&Debug', '&Release')
    $picked = $Host.UI.PromptForChoice('Build configuration', 'Select Debug or Release', $choices, 0)
    $Configuration = @('Debug', 'Release')[$picked]
}

if (-not $Deploy) {
    if ($PSBoundParameters.ContainsKey('DeployDir')) {
        $Deploy = $true # -DeployDir alone implies deploy
    }
    else {
        $choices = [System.Management.Automation.Host.ChoiceDescription[]]@('&No', '&Yes')
        if ($Host.UI.PromptForChoice('Build', 'Deploy after build?', $choices, 0) -eq 1) {
            $Deploy = $true
            $dir = Read-Host "Deploy dir (Enter = $DeployDir)"
            if ($dir) { $DeployDir = $dir }
        }
    }
}

dotnet build -c $Configuration (Join-Path $PSScriptRoot 'AutoLockerScreenEx.slnx')
if ($LASTEXITCODE -ne 0 -or -not $Deploy) { exit $LASTEXITCODE }

$binDir = Join-Path $PSScriptRoot "AutoLockerScreenEx\bin\$Configuration\net10.0-windows"
$targetExe = Join-Path $DeployDir 'AutoLockerScreenEx.exe'

$running = Get-Process -Name AutoLockerScreenEx -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $targetExe }
if ($running) {
    $choices = [System.Management.Automation.Host.ChoiceDescription[]]@('&Kill and continue', '&Abort deploy')
    $picked = $Host.UI.PromptForChoice('Deploy', "AutoLockerScreenEx is running from $DeployDir.", $choices, 0)
    if ($picked -ne 0) { Write-Host 'Deploy aborted.'; exit 1 }
    $running | Stop-Process -Force
    foreach ($proc in $running) {
        if (-not $proc.WaitForExit(10000)) {
            Write-Host "warning: pid $($proc.Id) did not exit within 10s"
        }
    }
}

New-Item -ItemType Directory -Force $DeployDir | Out-Null
$files = Get-ChildItem $binDir -Filter 'AutoLockerScreenEx.*' | Where-Object Extension -ne '.pdb'
# file locks can outlive process exit by a beat; retry instead of failing the deploy
for ($attempt = 0; $attempt -lt 5; $attempt++) {
    try {
        $files | Copy-Item -Destination $DeployDir -Force -ErrorAction Stop
        break
    }
    catch [System.IO.IOException] {
        if ($attempt -eq 4) { throw }
        Start-Sleep -Milliseconds 500
    }
}
Write-Host "Deployed to $DeployDir"

$choices = [System.Management.Automation.Host.ChoiceDescription[]]@('&Start', '&Exit')
if ($Host.UI.PromptForChoice('Deploy', "Start AutoLockerScreenEx from $DeployDir?", $choices, 0) -eq 0) {
    Start-Process $targetExe
}
exit 0
