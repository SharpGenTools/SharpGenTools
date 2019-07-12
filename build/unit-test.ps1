Param(
    [bool] $RunCodeCoverage = $true,
    [string] $Configuration = "Debug",
    [string] $RepoRoot
)


if(!(./build/Run-UnitTest -Project "SharpGen.UnitTests" -Configuration $Configuration -CollectCoverage $RunCodeCoverage -RepoRoot $RepoRoot)) {
    Write-Error "SharpGen Unit Tests Failed"
    return $false
}

if(!(./build/Run-UnitTest -Project "SharpGen.Runtime.UnitTests" -Configuration $Configuration -CollectCoverage $RunCodeCoverage -RepoRoot $RepoRoot)) {
    Write-Error "SharpGen.Runtime Unit Tests Failed"
    return $false
}

return $true