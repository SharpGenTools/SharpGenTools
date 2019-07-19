Param(
    [string] $AssemblyFolder,
    [string] $Assembly,
    [string] $Executable,
    [string[]] $Arguments,
    [string] $Output)


$ScriptFolder = Split-Path -parent $PSCommandPath

$targetArgs = $($Arguments -join ' ')

coverlet "$AssemblyFolder/SharpGenTools.Sdk.dll" -t $Executable -a $targetArgs -f opencover `
     -o $ScriptFolder/../artifacts/coverage/$Output --include-test-assembly --include-directory $AssemblyFolder `
     | Write-Host

return $LastExitCode -eq 0