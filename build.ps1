Param(
    [string] $Configuration = "Debug"
)

msbuild /p:Configuration=$Configuration

if ($LastExitCode -ne 0) {
    exit 1
}

dotnet pack -c $Configuration

if ($LastExitCode -ne 0) {
    exit 1
}
