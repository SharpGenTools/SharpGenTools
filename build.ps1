dotnet pack -c Release

if ($LastExitCode -ne 0) {
    exit 1
}
