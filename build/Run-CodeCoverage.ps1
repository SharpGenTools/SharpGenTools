Param(
    [string] $AssemblyFolder,
    [string] $Assembly,
    [string] $Executable,
    [string[]] $Arguments,
    [string[]] $Filters,
    [string] $Output)


$ScriptFolder = Split-Path -parent $PSCommandPath

$targetArgs = $($Arguments -join ' ')
$filter = $($Filters -join '%2c')

coverlet "$AssemblyFolder/SharpGenTools.Sdk.dll" -t $Executable -a $targetArgs --include $filter `
     -f opencover -o $ScriptFolder/../artifacts/coverage/$Output --include-test-assembly --include-directory $AssemblyFolder `
     | Write-Host

return $LastExitCode -eq 0