Param(
    [string] $Configuration = "Debug",
    [switch] $SkipUnitTests = $false,
    [switch] $SkipOuterloopTests = $false,
    [switch] $SkipCodeCoverage = $false
)

$RepoRoot = Split-Path -parent $PSCommandPath
mkdir $RepoRoot/artifacts/coverage -ErrorAction SilentlyContinue

$RunCodeCoverage = ($Configuration -eq "Debug") -and (-not $SkipCodeCoverage)

if (!$SkipUnitTests) {
    Write-Debug "Running Unit Tests"
    if (!(./build/unit-test $RunCodeCoverage $Configuration -RepoRoot $RepoRoot)) {
        Write-Error "Unit Tests Failed"
        exit 1
    }
}

if (!$SkipOuterloopTests -and !($env:ReleaseTag -and ($Configuration -eq "Release"))) {
    Write-Debug "Deploying test packages"
    if (!(./build/deploy-test-packages $Configuration)) {
        Write-Error "Failed to deploy test packages"
        exit 1
    }

    Write-Debug "Building outerloop native libraries"
    if (!(./build/build-outerloop-native)) {
        Write-Error "Failed to build outerloop native projects"
        exit 1
    }

    # Peter Franta, CC BY-SA 3.0, https://stackoverflow.com/a/10242325
    function CartesianProduct-Lists
    {
        [Diagnostics.CodeAnalysis.SuppressMessage("PSUseApprovedVerbs", "", Scope="function")]
        param($Lists)

        function Make-List
        {
            param($Head, $Tail)
            if ($Head -is [Object[]])
            {
                # List already so just extend
                $Result = $Head + $Tail
            }
            else
            {
                # Create List
                $Result = @($Head, $Tail)
            }
            ,$Result
        }

        # if Head..Tail
        if (@($Lists).Count -gt 1)
        {
            $Head = $Lists[0]
            $Next = $Lists[1]
            $Result = @()
            foreach ($HeadItem in $Head)
            {
                foreach ($NextItem in $Next)
                {
                    $Result += ,(Make-List $HeadItem $NextItem)
                }
            }
            if (@($Lists).Count -gt 2)
            {
                $Index = $Lists.Count - 1
                $Tail = $Lists[2..$Index]
                $Result = ,$Result + $Tail
                $Result = CartesianProduct-Lists $Result
            }
            ,$Result
        }
    }

    $managedTests = "Interface", "Struct", "Functions"

    $tfms = "net472", "netcoreapp2.1", "net5.0"
    $platforms = "x64" # "x86", "x64"

    $matrix = CartesianProduct-Lists @($tfms, $platforms)

    Write-Debug "Building and running outerloop tests"
    foreach ($testArgs in $matrix) {
        $tfm = $testArgs[0]
        $platform = $testArgs[1]

        $hint = "$tfm-$platform"
        $testArgs = "-p:TargetFramework=$tfm", "-p:TargetPlatform=$platform", "-p:Platform=$platform"

        if (!(./build/build-outerloop $RunCodeCoverage -Parameters $testArgs -Projects $managedTests -Hint $hint -RepoRoot $RepoRoot)) {
            Write-Error "Failed to build outerloop tests"
            exit 1
        }

        $testArgs += "--framework", $tfm

        if (!(./build/run-outerloop-tests $RunCodeCoverage -Parameters $testArgs -Projects $managedTests -Hint $hint -RepoRoot $RepoRoot -Platform $platform)) {
            Write-Error "Outerloop tests failed"
            exit 1
        }
    }
}
