Param(
    [string] $Configuration = "Debug"
)

if (Test-Path -Path "coverage.xml") {
    Remove-Item "coverage.xml"
}

$RunCodeCoverage = ($Configuration -eq "Debug")

Write-Debug "Running Unit Tests"
if (!(./build/unit-test $RunCodeCoverage $Configuration)) {
    Write-Error "Unit Tests Failed"
    exit 1
}

Write-Debug "Deploying test packages"
if(!(./build/deploy-test-packages $Configuration)) {
    Write-Error "Failed to deploy test packages"
    exit 1
}

Write-Debug "Building outerloop native libraries"
if(!(./build/build-outerloop-native)) {
    Write-Error "Failed to build outerloop native projects"
    exit 1
}

Write-Debug "Building outerloop tests"
if(!(./build/build-outerloop $RunCodeCoverage)) {
    Write-Error "Failed to build outerloop tests"
    exit 1
}

Write-Debug "Running outerloop tests"
if(!(./build/run-outerloop-tests)) {
    Write-Error "Outerloop tests failed"
    exit 1
}

if ($RunCodeCoverage -and $env:CI) {
    ./build/upload-coverage.ps1
}
