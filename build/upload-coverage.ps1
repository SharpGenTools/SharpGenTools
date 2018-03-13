Param(
    [string] $report = "coverage.xml"
)

codecov -f $report

return $LastExitCode -eq 0