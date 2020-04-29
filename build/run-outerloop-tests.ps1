Param(
    [bool] $RunCodeCoverage = $true,
    [string] $RepoRoot
)

$managedTests = "Interface", "Struct", "Functions"

foreach ($test in $managedTests) {
    if (!(./build/Run-UnitTest -Project $test -Configuration "Debug" -CollectCoverage $RunCodeCoverage -RepoRoot $RepoRoot -TestSubdirectory "SdkTests")) {
        Write-Error "Outer-loop $test Test Failed"
        return $false
    }
}

return $true