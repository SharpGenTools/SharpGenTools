Param([string] $executable, [string[]] $arguments, [string[]] $filters, [string] $output = "coverage.xml")

$targetArgs = $($arguments -join ' ')
$filter = $($filters -join ' ')

OpenCover.Console -register:user -oldstyle -returntargetcode -target:"$executable" -targetargs:"$targetArgs" -filter:"$filter" -mergeoutput -mergebyhash -output:$output -skipautoprops | Write-Host

return $LastExitCode -eq 0