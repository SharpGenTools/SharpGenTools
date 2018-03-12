Param(
    [bool] $RunOpenCover = $true
)

function Test
{
    Param([string] $project, [bool] $collectCoverage)

    $dotnetExe = & where.exe dotnet

    $arguments = @("test", "$project/$project.csproj", "--no-build", "--no-restore")

    $filters = @("+[SharpGen]*", "+[SharpGen.Runtime]*")

    if($collectCoverage) {
        return ./build/Run-OpenCover -executable $dotnetExe -arguments $arguments -filters $filters 
    }
    else {
        $tests = Start-Process -FilePath $dotnetExe -ArgumentList $arguments -WorkingDirectory $pwd.Path -PassThru -NoNewWindow -Wait
        return $tests.ExitCode -eq 0
    }

}

if(!(Test -project "SharpGen.UnitTests" -collectCoverage $RunOpenCover)) {
    return $false
}

if(!(Test -project "SharpGen.Runtime.UnitTests" -collectCoverage $RunOpenCover)) {
    return $false
}

return $true