Param(
    [bool] $RunCodeCoverage = $true,
    [string] $Configuration = "Debug",
    [string] $RepoRoot
)

return (./build/Run-UnitTest -Project "SharpGen.UnitTests" -Configuration $Configuration -CollectCoverage $RunCodeCoverage -RepoRoot $RepoRoot)