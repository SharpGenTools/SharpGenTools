msbuild SharpGenTools.sln /t:Restore /m
msbuild SharpGenTools.sln /p:Configuration=Release /m

if ($LastExitCode -ne 0) {
    exit 1
}
pushd SharpGenTools.Sdk
msbuild /t:Pack /p:Configuration=Release
popd
