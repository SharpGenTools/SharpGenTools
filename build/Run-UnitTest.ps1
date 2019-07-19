Param(
    [string] $Project,
    [string] $Configuration,
    [bool] $CollectCoverage,
    [string] $RepoRoot,
    [string] $TestSubdirectory = "",
    [string] $CoverageIncludeDirectory = ""
)

dotnet test "$RepoRoot/$TestSubdirectory/$Project/$Project.csproj" --no-build --no-restore -c $Configuration /p:CollectCoverage=$CollectCoverage `
    /p:Include='[SharpGen]*%2c[SharpGen.Runtime]*' /p:IncludeDirectory="$CoverageIncludeDirectory" `
    /p:CoverletOutputFormat=opencover /p:CoverletOutput="$RepoRoot/artifacts/coverage/$Project.xml" `
    --logger "trx;LogFileName=$RepoRoot/artifacts/test-results/$Project.trx" `
    | Write-Host
return $LastExitCode -eq 0
