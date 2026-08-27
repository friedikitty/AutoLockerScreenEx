# build.ps1 [-Configuration Debug|Release]
# Builds the solution. Without -Configuration, asks via a console menu.
param([string]$Configuration = '')

if ($Configuration -notin 'Debug', 'Release') {
    $choices = [System.Management.Automation.Host.ChoiceDescription[]]@('&Debug', '&Release')
    $picked = $Host.UI.PromptForChoice('Build configuration', 'Select Debug or Release', $choices, 0)
    $Configuration = @('Debug', 'Release')[$picked]
}

dotnet build -c $Configuration (Join-Path $PSScriptRoot 'AutoLockerScreenEx.slnx')
exit $LASTEXITCODE
