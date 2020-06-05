$ScriptRoot = Split-Path -parent $PSCommandPath
ls $ScriptRoot/../artifacts/coverage/*.xml | %{ codecov -f $_.FullName }

return $LastExitCode -eq 0