Param(
    [string] $Configuration = "Debug"
)

dotnet build -c $Configuration

if ($LastExitCode -ne 0) {
    exit 1
}

dotnet pack -c $Configuration

if ($LastExitCode -ne 0) {
    exit 1
}
