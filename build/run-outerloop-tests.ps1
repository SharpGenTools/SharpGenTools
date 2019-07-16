Param(
    [bool] $RunCodeCoverage = $true,
    [string] $RepoRoot
)

$SharpGenVersion = ./build/Get-SharpGenToolsVersion

$SharpGenRuntimePath = "$RepoRoot/SdkTests/RestoredPackages/sharpgen.runtime/$SharpGenVersion/runtimes/win/lib/netstandard2.0/"

$managedTests = "Interface", "Struct", "Functions"

foreach($test in $managedTests) {
    ./build/Run-UnitTest -Project $test -Configuration "Debug" -CollectCoverage $RunCodeCoverage `
         -RepoRoot $RepoRoot -TestSubdirectory "SdkTests" -CoverageIncludeDirectory $SharpGenRuntimePath
    if ($LastExitCode -ne 0) {
        return $false
    }
}

return $true