Param(
    [string] $Configuration = "Debug"
)

dotnet pack -c $Configuration

if ($LastExitCode -ne 0) {
    exit 1
}
