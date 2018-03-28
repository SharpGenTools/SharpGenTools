$managedTests = "Interface", "Struct", "Functions"

foreach($test in $managedTests) {
    dotnet test ./SdkTests/$test/$test.csproj --no-build --no-restore | Write-Host
    if ($LastExitCode -ne 0) {
        return $false
    }
}

return $true