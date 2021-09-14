Param(
    [Parameter(Mandatory=$true)][string] $RepoRoot
)

Import-Module $RepoRoot/build/VSSetup/Microsoft.VisualStudio.Setup.PowerShell.dll

$VSInstance = Get-VSSetupInstance -Prerelease | Select-VSSetupInstance -Latest -Product * -Require 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64'

if (!$VSInstance) {
    Write-Error "Failed to find a proper VS instance"
    return $false
}

if ($VSInstance.InstallationVersion -lt [Version]::new(16, 3)) {
    Write-Error "Installed VS instance is too old"
    return $false
}

$DevShellPath = "$($VSInstance.InstallationPath)\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"

if (!(Test-Path $DevShellPath)) {
    Write-Error "Installed VS instance doesn't contain PowerShell DevShell module"
    return $false
}

Import-Module $DevShellPath

Push-Location SdkTests/Native

$OriginalEnv = Get-ChildItem Env:

$generator = "Ninja"

try {
    if (!(Test-Path x86)) {
        New-Item -Type Directory -Name x86
    }

    Push-Location x86

    try {
        Enter-VsDevShell -InstanceId $VSInstance.InstanceId -SkipAutomaticLocation -DevCmdArguments "-no_logo -arch=x86 -host_arch=x64"

        cmake -S .. -B . -G $generator | Write-Host

        if ($LastExitCode -ne 0) {
            Write-Error "Failed to generate x86 native projects"
            return $false
        }

        cmake --build . | Write-Host

        if ($LastExitCode -ne 0) {
            Write-Error "Failed to build x86 native projects"
            return $false
        }
    } finally {
        Pop-Location
        Remove-Item Env:* -Force
        $OriginalEnv | ForEach-Object { Set-Item "Env:$($_.Name)" $_.Value }
    }

    if (!(Test-Path x64)) {
        New-Item -Type Directory -Name x64
    }

    Push-Location x64

    try {
        Enter-VsDevShell -InstanceId $VSInstance.InstanceId -SkipAutomaticLocation -DevCmdArguments "-no_logo -arch=x64 -host_arch=x64"

        cmake -S .. -B . -G $generator | Write-Host

        if ($LastExitCode -ne 0) {
            Write-Error "Failed to generate x64 native projects"
            return $false
        }

        cmake --build . | Write-Host

        if ($LastExitCode -ne 0) {
            Write-Error "Failed to build x64 native projects"
            return $false
        }
    } finally {
        Pop-Location
        Remove-Item Env:* -Force
        $OriginalEnv | ForEach-Object { Set-Item "Env:$($_.Name)" $_.Value }
    }

    return $true
} finally {
    Pop-Location
}