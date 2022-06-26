// See https://aka.ms/new-console-template for more information

// I'm a dummy project for testing trimmability, since the analyzer outside of publish time isn't fully perfect yet
// test my trimming with `dotnet publish -r win-x64`

Console.WriteLine("Hello SharpGenTools");

// Please note that you'll need to `dotnet pack` SharpGenTools.Sdk and
// put it in the `SharpGen.Runtime.COM/LocalPackages` package for SharpGen.Runtime.COM
// dependency to compile.