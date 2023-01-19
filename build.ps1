Param(
    [string] $Configuration = "Debug",
    [switch] $SkipUnitTests = $false,
    [switch] $SkipOuterloopTests = $false
)

$env:MSBuildEnableWorkloadResolver=$false

dotnet pack -c $Configuration

if ($LastExitCode -ne 0) {
    exit 1
}

Write-Debug "Deploying built packages for COM Runtime build"
if (!(./build/deploy-test-packages -PackedConfiguration $Configuration -Project "SharpGen.Runtime.COM/SharpGen.Runtime.COM")) {
    Write-Error "Failed to deploy packages"
    exit 1
}

$RepoRoot = Split-Path -parent $PSCommandPath

Write-Debug "COM Runtime build"
if (!(./build/build-com-runtime -Configuration $Configuration -RepoRoot $RepoRoot)) {
    Write-Error "COM Runtime build failed"
    exit 1
}

mkdir $RepoRoot/artifacts/coverage -ErrorAction SilentlyContinue

if (!$SkipUnitTests) {
    Write-Debug "Running Unit Tests"
    if (!(./build/Run-UnitTest -Target "$RepoRoot/SharpGen.UnitTests/SharpGen.UnitTests.csproj" -Name "UnitTests" -Configuration $Configuration -RepoRoot $RepoRoot)) {
        Write-Error "Unit Tests Failed"
        exit 1
    }
}

if (!$SkipOuterloopTests -and !($env:ReleaseTag -and ($Configuration -eq "Release"))) {
    Write-Debug "Deploying test packages"
    if (!(./build/deploy-test-packages -PackedConfiguration $Configuration -Project "SdkTests")) {
        Write-Error "Failed to deploy test packages"
        exit 1
    }

    Write-Debug "Building outerloop native libraries"
    if (!(./build/build-outerloop-native -RepoRoot $RepoRoot)) {
        Write-Error "Failed to build outerloop native projects"
        exit 1
    }

    $SdkVersion = & "$RepoRoot/build/Get-SharpGenToolsVersion"

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

    $tfms = "net472", "netcoreapp3.1", "net6.0"
    $platforms = "x86", "x64"

    $matrix = CartesianProduct-Lists @($tfms, $platforms)

    Write-Debug "Building and running outerloop tests"
    foreach ($testArgs in $matrix) {
        $tfm = $testArgs[0]
        $platform = $testArgs[1]

        if (!(./build/run-outerloop-tests -Projects $managedTests -TargetFramework $tfm -Platform $platform -Version $SdkVersion -RepoRoot $RepoRoot)) {
            Write-Error "Outerloop tests failed"
            exit 1
        }
    }
}
