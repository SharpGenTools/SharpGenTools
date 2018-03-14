cmake SdkTests/Native -A x64 | Write-Host

if ($LastExitCode -ne 0) {
    Write-Error "Failed to generate native projects"
    return $false
}

cmake --build SdkTests/Native | Write-Host

if ($LastExitCode -ne 0) {
    Write-Error "Failed to build native projects"
    return $false
}

return $true