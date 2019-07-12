
$ScriptFolder = Split-Path -parent $PSCommandPath
$VersionDumpFile = "$ScriptFolder/../artifacts/version.txt"

dotnet msbuild $ScriptFolder/version.proj /p:VersionDumpFile=$VersionDumpFile | Out-Null

$Version = Get-Content $VersionDumpFile -TotalCount 1

return $Version