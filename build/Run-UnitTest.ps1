Param(
    [Parameter(Mandatory=$true)][string] $Target,
    [Parameter(Mandatory=$true)][string] $Name,
    [Parameter(Mandatory=$true)][string] $Configuration,
    [Parameter(Mandatory=$true)][string] $RepoRoot,
    [string[]] $Parameters
)

dotnet test $Target -c $Configuration /bl:"$RepoRoot/artifacts/binlog/$Name.binlog" $Parameters `
    /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:IncludeTestAssembly=true `
    /p:Include='[SharpGen]*%2c[SharpGen.Runtime]*%2c[SharpGen.Platform]*%2c[SharpGenTools.Sdk]*' `
    /p:CoverletOutput="$RepoRoot/artifacts/coverage/$Name.xml" `
    --logger "trx;LogFileName=$RepoRoot/artifacts/test-results/$Name.trx" | Write-Host

return $LastExitCode -eq 0
