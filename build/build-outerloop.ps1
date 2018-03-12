Param(
    [bool] $RunOpenCover = $true
)

# Add directory to path for sn executable
$env:Path += ";C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\"

$msbuildLocations = & where.exe msbuild
$msbuildExe = $null

if ($msbuildLocations -eq $null) {
    Write-Error "Unable to locate MSBuild"
    return $false
}
elseif ($msbuildLocations -is [array]) {
    $msbuildExe = $msbuildLocations[0]
}
else {
    $msbuildExe = $msbuildLocations
}

$msbuildParameters = "./SdkTests/SdkTests.sln", "/restore", "/m", "/v:n"
$coverageFilter = @("+[SharpGen]*")

if ($RunOpenCover) {
    return (./build/Run-OpenCover $msbuildExe $msbuildParameters $coverageFilter)
}
else {
    $tests = Start-Process -FilePath $msBuildExe -ArgumentList $msbuildParameters -WorkingDirectory $pwd.Path -PassThru -NoNewWindow -Wait
    return $tests.ExitCode -eq 0
}
 