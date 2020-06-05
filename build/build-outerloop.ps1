Param(
    [bool] $RunCodeCoverage = $true,
    [string] $RepoRoot
)

# Add directory to path for sn executable
$env:Path += ";C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\"

$SdkTestsSolution = "$RepoRoot/SdkTests/SdkTests.sln"

dotnet restore $SdkTestsSolution | Write-Host

if ($RunCodeCoverage) {
    $Version = & "$RepoRoot/build/Get-SharpGenToolsVersion"

    $SdkAssemblyFolder = "$RepoRoot/SdkTests/RestoredPackages/sharpgentools.sdk/$Version/tools/"

    $dotnetExe = $(Get-Command dotnet).Path

    $dotnetParameters = "build", $SdkTestsSolution, "-nr:false", "-m:1" # Force no parallelization to work around https://github.com/tonerdo/coverlet/issues/491

    $targetArgs = $($dotnetParameters -join ' ')

    coverlet "$SdkAssemblyFolder/netcoreapp2.1/win/SharpGenTools.Sdk.dll" -t $dotnetExe -a $targetArgs -f opencover `
        -o "$RepoRoot/artifacts/coverage/outerloop-build.xml" --include-test-assembly --include-directory $SdkAssemblyFolder `
        | Write-Host
} else {
    dotnet build -nr:false $SdkTestsSolution | Write-Host
}

return $LastExitCode -eq 0
