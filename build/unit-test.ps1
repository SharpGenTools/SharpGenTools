Param(
    [bool] $RunCodeCoverage = $true,
    [string] $Configuration = "Debug",
    [string] $RepoRoot
)

function Test
{
    Param([string] $project, [bool] $collectCoverage)

    dotnet test "$project/$project.csproj" --no-build --no-restore -c $Configuration /p:CollectCoverage=$collectCoverage `
        /p:Include='[SharpGen]*%2c[SharpGen.Runtime]*' /p:CoverletOutputFormat=opencover /p:CoverletOutput="$RepoRoot/artifacts/coverage/$project.xml" `
        | Write-Host
    return $LastExitCode -eq 0

}

if(!(Test -project "SharpGen.UnitTests" -collectCoverage $RunCodeCoverage)) {
    Write-Error "SharpGen Unit Tests Failed"
    return $false
}

if(!(Test -project "SharpGen.Runtime.UnitTests" -collectCoverage $RunCodeCoverage)) {
    Write-Error "SharpGen.Runtime Unit Tests Failed"
    return $false
}

return $true