$env:Path += ";C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\"

pushd SharpGen.E2ETests
    dotnet xunit
    if ($LastExitCode -ne 0) {
        exit 1
    }
popd


if(Test-Path -Path SdkTests/RestoredPackages/sharpgentools.sdk){
    rm -r -Force SdkTests/RestoredPackages/sharpgentools.sdk
    rm -r -Force SdkTests/RestoredPackages/sharpgen.runtime -ErrorAction SilentlyContinue
}

mkdir SdkTests/LocalPackages -ErrorAction SilentlyContinue
rm SdkTests/LocalPackages/*.nupkg
cp SharpGenTools.Sdk/bin/Release/*.nupkg SdkTests/LocalPackages/

pushd .\SdkTests
    msbuild /t:Restore /v:minimal

    if ($LastExitCode -ne 0) {
        exit 1
    }

    msbuild /p:Configuration=Release /m /v:minimal

    if ($LastExitCode -ne 0) {
        exit 1
    }

    pushd SharpGen.Runtime
        msbuild /t:Pack /p:Configuration=Release /v:minimal
        cp bin/Release/*.nupkg ../LocalPackages
    popd

    pushd ComInterface
        pushd ComLibTest
            dotnet test --no-build --no-restore -c Release
            if ($LastExitCode -ne 0) {
                exit 1
            }
        popd

        msbuild ComLibTest.Package/ComLibTest.Package.csproj /t:Restore /v:minimal

        if ($LastExitCode -ne 0) {
            exit 1
        }

        msbuild ComLibTest.Package/ComLibTest.Package.csproj /p:Configuration=Release /v:minimal

        if ($LastExitCode -ne 0) {
            exit 1
        }
    popd

popd
