Param(
    [Parameter(Mandatory=$true)][string] $Configuration,
    [Parameter(Mandatory=$true)][string] $RepoRoot,
    [string[]] $AdditionalParameters
)

Remove-Item -Recurse -Force "$RepoRoot/SharpGen.Runtime.COM/SharpGen.Runtime.COM/bin/$Configuration" -ErrorAction Ignore
Remove-Item -Recurse -Force "$RepoRoot/SharpGen.Runtime.COM/SharpGen.Runtime.COM/obj/$Configuration" -ErrorAction Ignore

$ComRuntimeProject = "$RepoRoot/SharpGen.Runtime.COM/SharpGen.Runtime.COM/SharpGen.Runtime.COM.csproj"

$Parameters = @(
    "pack", "-bl:$RepoRoot/artifacts/binlog/com-runtime-$Configuration.binlog",
    "-nr:false", "-c:$Configuration", "-v:m"
) + $AdditionalParameters + @($ComRuntimeProject)

dotnet $Parameters | Write-Host

if ($LastExitCode -ne 0) {
    Write-Error "Failed to pack $Configuration COM runtime"
    return $false
}

return $true