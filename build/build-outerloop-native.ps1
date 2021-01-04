Push-Location SdkTests/Native

$generator = "Visual Studio 16 2019"

try {
    if (!(Test-Path x86)) {
        New-Item -Type Directory -Name x86
    }

    Push-Location x86

    try {
        cmake -S .. -B . -G $generator -A Win32 | Write-Host

        if ($LastExitCode -ne 0) {
            Write-Error "Failed to generate x86 native projects"
            return $false
        }

        cmake --build . | Write-Host

        if ($LastExitCode -ne 0) {
            Write-Error "Failed to build x86 native projects"
            return $false
        }
    } finally {
        Pop-Location
    }

    if (!(Test-Path x64)) {
        New-Item -Type Directory -Name x64
    }

    Push-Location x64

    try {
        cmake -S .. -B . -G $generator -A x64 | Write-Host

        if ($LastExitCode -ne 0) {
            Write-Error "Failed to generate x64 native projects"
            return $false
        }

        cmake --build . | Write-Host

        if ($LastExitCode -ne 0) {
            Write-Error "Failed to build x64 native projects"
            return $false
        }
    } finally {
        Pop-Location
    }

    return $true
} finally {
    Pop-Location
}