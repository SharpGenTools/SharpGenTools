Param(
    [Parameter(Mandatory=$true)][string[]] $Projects,
    [Parameter(Mandatory=$true)][string] $TargetFramework,
    [Parameter(Mandatory=$true)][string] $Platform,
    [Parameter(Mandatory=$true)][string] $Version,
    [Parameter(Mandatory=$true)][string] $RepoRoot,
    [string[]] $AdditionalParameters
)

$Hint = "$TargetFramework-$Platform"
$Parameters = @("-p:TargetFramework=$TargetFramework", "-p:TargetPlatform=$Platform", "-p:Platform=$Platform") + $AdditionalParameters

foreach ($test in $Projects) {
    Remove-Item -Recurse -Force "$RepoRoot/SdkTests/$test/bin" -ErrorAction Ignore
    Remove-Item -Recurse -Force "$RepoRoot/SdkTests/$test/obj" -ErrorAction Ignore
}

$SdkTestsSolution = "$RepoRoot/SdkTests/SdkTests.sln"

# Force no parallelization to work around https://github.com/coverlet-coverage/coverlet/issues/491
$Parameters = @("-nr:false", "-m:1", "-v:m") + $Parameters

$RestoreParameters = @("restore", "-bl:$RepoRoot/artifacts/binlog/outerloop-restore-$Hint.binlog") + $Parameters + @($SdkTestsSolution)

dotnet $RestoreParameters | Write-Host

if ($LastExitCode -ne 0) {
    Write-Error "Failed to restore outerloop tests dependencies"
    return $false
}

$RunSettings = "--", "RunConfiguration.TargetPlatform=$Platform"

if ($TargetFramework -eq "net472") {
    $RunSettings += "RunConfiguration.DisableAppDomain=true"
} else {
    $Parameters += @(
        "/p:CollectCoverage=true", "/p:CoverletOutputFormat=opencover", "/p:IncludeTestAssembly=true",
        "/p:Include='[SharpGen]*%2c[SharpGen.Runtime]*%2c[SharpGen.Platform]*%2c[SharpGenTools.Sdk]*'",
        "/p:CoverletOutput=$RepoRoot/artifacts/coverage/outerloop-test-$Hint.xml"
    )
}

$BuildParameters = @(
    "test", "--no-restore", "-c:Debug", "-bl:$RepoRoot/artifacts/binlog/outerloop-test-$Hint.binlog", "--framework", $TargetFramework,
    "--logger", "trx;LogFileName=$RepoRoot/artifacts/test-results/outerloop-test-$Hint.trx"
) + $Parameters + @($SdkTestsSolution) + $RunSettings

$SdkAssemblyFolder = "$RepoRoot/SdkTests/RestoredPackages/sharpgentools.sdk/$Version/tools/"

$dotnetExe = $(Get-Command dotnet).Path
$dotnetArgs = $($BuildParameters -join ' ')

dotnet coverlet "$SdkAssemblyFolder/net6.0/win/SharpGenTools.Sdk.dll" -t $dotnetExe -a $dotnetArgs -f opencover `
    -o "$RepoRoot/artifacts/coverage/outerloop-host-$Hint.xml" --include-test-assembly --include-directory $SdkAssemblyFolder `
    | Write-Host

if ($LastExitCode -ne 0) {
    Write-Error "Failed to build or run $hint outerloop tests"
    return $false
}

return $true