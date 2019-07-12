Param(
    [bool] $RunCodeCoverage = $true,
    [string] $RepoRoot
)

dotnet restore ./SdkTests/SdkTests.sln | Write-Host

$Version = ./build/Get-SharpGenToolsVersion

$SdkAssemblyFolder = "$RepoRoot/SdkTests/RestoredPackages/sharpgentools.sdk/$Version/tools/netstandard1.3/" 

# Add directory to path for sn executable
$env:Path += ";C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\"

$dotnetExe = $(Get-Command dotnet).Path

$dotnetParameters =  "build", "./SdkTests/SdkTests.sln", "/nodeReuse:false", "--no-restore"
$coverageFilter = "[SharpGen]*", "[SharpGenTools.Sdk]*"

if ($RunCodeCoverage) {
    return (./build/Run-CodeCoverage $SdkAssemblyFolder "SharpGenTools.Sdk.dll" $dotnetExe $dotnetParameters $coverageFilter "outerloop-build.xml")
}
else {
    dotnet build ./SdkTests/SdkTests.sln | Write-Host
    return $LastExitCode -eq 0
}
 