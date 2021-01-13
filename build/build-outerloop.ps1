Param(
    [string[]] $Parameters,
    [string[]] $Projects,
    [string] $Hint,
    [bool] $RunCodeCoverage = $true,
    [string] $RepoRoot
)

$netFxRoot = "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin"

# Add directory to path for sn executable
if ($env:Path -notmatch [Regex]::Escape($netFxRoot)){
    # TODO: Find proper subdirectory instead of hardcoded 4.6.1
    $netFxPath = "$netFxRoot\NETFX 4.6.1 Tools\"
    if (Test-Path $netFxPath) {
        $env:Path += ";$netFxPath"
    }
}

foreach ($test in $Projects) {
    Remove-Item -Recurse -Force "$RepoRoot/SdkTests/$test/bin" -ErrorAction Ignore
    Remove-Item -Recurse -Force "$RepoRoot/SdkTests/$test/obj" -ErrorAction Ignore
}

$SdkTestsSolution = "$RepoRoot/SdkTests/SdkTests.sln"

$Parameters = @($SdkTestsSolution, "-nr:false") + $Parameters

if ($RunCodeCoverage) {
    # Force no parallelization to work around https://github.com/tonerdo/coverlet/issues/491
    $Parameters += "-m:1"
}

$RestoreParameters = $Parameters + @("-bl:$RepoRoot/artifacts/binlog/restore-outerloop-$Hint.binlog")
$BuildParameters = @("build", "-c:Debug", "-bl:$RepoRoot/artifacts/binlog/build-outerloop-$Hint.binlog") + $Parameters

if ($RunCodeCoverage) {
    dotnet restore $RestoreParameters | Write-Host

    $Version = & "$RepoRoot/build/Get-SharpGenToolsVersion"

    $SdkAssemblyFolder = "$RepoRoot/SdkTests/RestoredPackages/sharpgentools.sdk/$Version/tools/"

    $dotnetExe = $(Get-Command dotnet).Path

    $dotnetArgs = $($BuildParameters -join ' ')

    coverlet "$SdkAssemblyFolder/netcoreapp2.1/win/SharpGenTools.Sdk.dll" -t $dotnetExe -a $dotnetArgs -f opencover `
        -o "$RepoRoot/artifacts/coverage/outerloop-build-$Hint.xml" --include-test-assembly --include-directory $SdkAssemblyFolder `
        | Write-Host
} else {
    dotnet $BuildParameters | Write-Host
}

return $LastExitCode -eq 0
