Param([string] $exe)

$exeLocations = & where.exe $exe

if ($exeLocations -eq $null) {
    Write-Error "Unable to locate $exe"
    return $false
}
elseif ($exeLocations -is [array]) {
    return $exeLocations[0]
}
else {
    return $exeLocations
}