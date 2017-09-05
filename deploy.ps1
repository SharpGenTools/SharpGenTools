if(!($env:APPVEYOR_PULL_REQUEST_NUMBER)) {
    foreach ($artifactName in $artifacts.keys) {
        dotnet nuget push $artifacts[$artifactName].path -k $env:MYGET_API_KEY -s https://www.myget.org/F/sharpgentools/api/v2/package 
        if($env:APPVEYOR_REPO_TAG) {
            dotnet nuget push $artifacts[$artifactName].path -k $env:NUGET_API_KEY -s https://www.nuget.org/api/v2/package
        }
    }
}