Param(
    [Parameter(Mandatory = $true)]
    [string] $PackedConfiguration
)

$localPackagesFolder = "SdkTests/LocalPackages"
$restorePackagesFolder = "SdkTests/RestoredPackages"
$sdkPackages = "SharpGenTools.Sdk", "SharpGen.Runtime"

if (!(Test-Path -Path $localPackagesFolder)) {
    mkdir $localPackagesFolder
}

Remove-Item SdkTests/LocalPackages/*.nupkg

foreach ($sdkPackage in $sdkPackages) {
    Copy-Item $sdkPackage/bin/$PackedConfiguration/*.nupkg $localPackagesFolder
}

if (!(Test-Path -Path $restorePackagesFolder)) {
    mkdir $restorePackagesFolder
}

try {
    foreach ($sdkPackage in $sdkPackages) {
        $restoreFolderName = $sdkPackage.ToLower()
        $restorePath = "$restorePackagesFolder/$restoreFolderName"
        if (Test-Path -Path $restorePath) {
            Remove-Item -Recurse -Force $restorePath -ErrorAction Stop
        }
    }
}
catch {
    Write-Error $_.Exception.Message
    return $false
}

return $true