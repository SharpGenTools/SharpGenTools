Param(
    [bool] $RunCodeCoverage = $true,
    [string] $Configuration = "Debug",
    [string] $RepoRoot
)

$managedTests = "SharpGen.UnitTests", "SharpGen.Runtime.UnitTests"

foreach ($test in $managedTests) {
    if (!(./build/Run-UnitTest -Project $test -Configuration $Configuration -CollectCoverage $RunCodeCoverage -RepoRoot $RepoRoot)) {
        Write-Error "$test Failed"
        return $false
    }
}

return $true