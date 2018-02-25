$env:Path += ";C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\"

pushd SharpGen.UnitTests
    dotnet xunit
    if ($LastExitCode -ne 0) {
        exit 1
    }
popd


if(Test-Path -Path SdkTests/RestoredPackages/sharpgentools.sdk){
    rm -r -Force SdkTests/RestoredPackages/sharpgentools.sdk
}

if(Test-Path -Path SdkTests/RestoredPackages/sharpgen.doc.msdn.tasks){
    rm -r -Force SdkTests/RestoredPackages/sharpgen.doc.msdn.tasks
}

if(Test-Path -Path SdkTests/RestoredPackages/sharpgen.runtime){
    rm -r -Force SdkTests/RestoredPackages/sharpgen.runtime
}

mkdir SdkTests/LocalPackages -ErrorAction SilentlyContinue
rm SdkTests/LocalPackages/*.nupkg
cp SharpGenTools.Sdk/bin/Release/*.nupkg SdkTests/LocalPackages/
cp SharpGen.Doc.Msdn.Tasks/bin/Release/*.nupkg SdkTests/LocalPackages/
cp SharpGen.Runtime/bin/Release/*.nupkg SdkTests/LocalPackages/

pushd .\SdkTests
    msbuild /t:Restore /v:minimal

    if ($LastExitCode -ne 0) {
        exit 1
    }

    msbuild /p:Configuration=Release /m /v:n

    if ($LastExitCode -ne 0) {
        exit 1
    }

    pushd ComInterface
        pushd ComLibTest
            dotnet test --no-build --no-restore -c Release
            if ($LastExitCode -ne 0) {
                exit 1
            }
        popd
    popd

popd
