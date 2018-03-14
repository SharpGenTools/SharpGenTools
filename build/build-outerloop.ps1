Param(
    [bool] $RunOpenCover = $true
)

# Add directory to path for sn executable
$env:Path += ";C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\"

$dotnetExe = ./build/find-exe "dotnet"

$dotnetParameters =  "build", "./SdkTests/SdkTests.sln"
$coverageFilter = @("+[SharpGen]*")

if ($RunOpenCover) {
    return (./build/Run-OpenCover $dotnetExe $dotnetParameters $coverageFilter)
}
else {
    dotnet build ./SdkTests/SdkTests.sln | Write-Host
    return $LastExitCode -eq 0
}
 