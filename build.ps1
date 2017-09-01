msbuild SharpGenTools.sln /t:Restore /m /v:minimal
msbuild SharpGenTools.sln /p:Configuration=Release /m /v:minimal

if ($LastExitCode -ne 0) {
    exit 1
}
msbuild /t:Pack /p:Configuration=Release /v:minimal
