$infrastructure = ".vs", "LocalPackages", "RestoredPackages", "x64", "build"

foreach($test in Get-ChildItem -Path SdkTests -Directory -Name) {
    if ($infrastructure -notcontains $test) {
        dotnet test ./SdkTests/$test/$test/$test.csproj --no-build --no-restore | Write-Host
        if ($LastExitCode -ne 0) {
            return $false
        }
    }
}

return $true