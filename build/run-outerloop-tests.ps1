Param(
    [Parameter(Mandatory=$true)][string[]] $Parameters,
    [Parameter(Mandatory=$true)][string[]] $Projects,
    [Parameter(Mandatory=$true)][string] $Hint,
    [Parameter(Mandatory=$true)][string] $Platform,
    [bool] $RunCodeCoverage = $true,
    [Parameter(Mandatory=$true)][string] $RepoRoot
)

$RunSettings = "--", "RunConfiguration.TargetPlatform=$Platform"

foreach ($test in $Projects) {
    if (!(./build/Run-UnitTest -Project $test -Configuration "Debug" -CollectCoverage $RunCodeCoverage -RepoRoot $RepoRoot -Hint "-$Hint" -Parameters $Parameters -TestSubdirectory "SdkTests" -RunSettings $RunSettings)) {
        Write-Error "Outer-loop $test Test Failed"
        return $false
    }
}

return $true