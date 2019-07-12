Param(
    [string] $Configuration = "Debug",
    [switch] $SkipUnitTests = $false,
    [switch] $SkipOuterloopTests = $false
)

$RepoRoot = Split-Path -parent $PSCommandPath
mkdir $RepoRoot/artifacts/coverage -ErrorAction SilentlyContinue

if (Test-Path -Path "coverage.xml") {
    Remove-Item "coverage.xml"
}

$RunCodeCoverage = ($Configuration -eq "Debug")

if(!($SkipUnitTests)) {
    Write-Debug "Running Unit Tests"
    if (!(./build/unit-test $RunCodeCoverage $Configuration -RepoRoot $RepoRoot)) {
        Write-Error "Unit Tests Failed"
        exit 1
    }
}

if(!($SkipOuterloopTests)) {
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
    if(!(./build/build-outerloop $RunCodeCoverage -RepoRoot $RepoRoot)) {
        Write-Error "Failed to build outerloop tests"
        exit 1
    }

    Write-Debug "Running outerloop tests"
    if(!(./build/run-outerloop-tests $RunCodeCoverage -RepoRoot $RepoRoot)) {
        Write-Error "Outerloop tests failed"
        exit 1
    }
}

if ($RunCodeCoverage -and $env:CI) {
    ./build/upload-coverage.ps1
}
