Param(
    [Parameter(Mandatory=$true)][string] $Project,
    [Parameter(Mandatory=$true)][string] $Configuration,
    [bool] $CollectCoverage,
    [Parameter(Mandatory=$true)][string] $RepoRoot,
    [string[]] $Parameters,
    [string] $Hint = "",
    [string] $TestSubdirectory = "",
    [string[]] $RunSettings
)

dotnet test "$RepoRoot/$TestSubdirectory/$Project/$Project.csproj" --no-build --no-restore -c $Configuration `
    /bl:"$RepoRoot/artifacts/binlog/$Project$Hint.binlog" $Parameters `
    /p:CollectCoverage=$CollectCoverage /p:CoverletOutputFormat=opencover /p:IncludeTestAssembly=true `
    /p:Include='[SharpGen]*%2c[SharpGen.Runtime]*%2c[SharpGen.Platform]*%2c[SharpGenTools.Sdk]*%2c[SharpPatch]*' `
    /p:CoverletOutput="$RepoRoot/artifacts/coverage/$Project$Hint.xml" `
    --logger "trx;LogFileName=$RepoRoot/artifacts/test-results/$Project$Hint.trx" `
    $RunSettings | Write-Host

return $LastExitCode -eq 0
