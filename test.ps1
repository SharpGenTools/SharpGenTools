dotnet test SharpGen.UnitTests/SharpGen.UnitTests.csproj
if ($LastExitCode -ne 0) {
    exit 1
}

$localPackagesFolder = "SdkTests/LocalPackages"
$restorePackagesFolder = "SdkTests/RestoredPackages"
$sdkPackages = "SharpGenTools.Sdk", "SharpGen.Doc.Msdn.Tasks", "SharpGen.Runtime"

if (!(Test-Path -Path $localPackagesFolder)) {
    mkdir $localPackagesFolder
}

Remove-Item SdkTests/LocalPackages/*.nupkg

foreach ($sdkPackage in $sdkPackages) {
    Copy-Item $sdkPackage/bin/Release/*.nupkg $localPackagesFolder
}

if (!(Test-Path -Path $restorePackagesFolder)) {
    mkdir $restorePackagesFolder
}

foreach ($sdkPackage in $sdkPackages) {
    $restoreFolderName = $sdkPackage.ToLower()
    $restorePath = "$restorePackagesFolder/$restoreFolderName"
    if (Test-Path -Path $restorePath) {
        Remove-Item -Recurse -Force $restorePath
    }
}

 # Add directory to path for sn executable
 $env:Path += ";C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\"

msbuild ./SdkTests/SdkTests.sln /restore /p:Configuration=Release /m /v:n

if ($LastExitCode -ne 0) {
    exit 1
}

$infrastructure = ".vs", "LocalPackages", "RestoredPackages", "x64"

foreach($test in Get-ChildItem -Path SdkTests -Directory -Name) {
    if ($infrastructure -notcontains $test) {
        dotnet test ./SdkTests/$test/$test/$test.csproj --no-build --no-restore -c Release
        if ($LastExitCode -ne 0) {
            exit 1
        }
    }
}
