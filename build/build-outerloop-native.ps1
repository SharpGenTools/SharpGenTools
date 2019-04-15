Push-Location SdkTests/Native

try {
    cmake . -A x64 | Write-Host

    if ($LastExitCode -ne 0) {
        Write-Error "Failed to generate native projects"
        return $false
    }

    cmake --build . | Write-Host

    if ($LastExitCode -ne 0) {
        Write-Error "Failed to build native projects"
        return $false
    }
}
finally {
    Pop-Location
}

return $true
