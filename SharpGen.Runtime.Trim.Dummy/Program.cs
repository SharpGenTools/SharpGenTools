// See https://aka.ms/new-console-template for more information

// I'm a dummy project for testing trimmability, since the analyzer outside of publish time isn't fully perfect yet
// test my trimming with `dotnet publish -r win-x64`

using SharpGen.Runtime.Trim.Dummy.CallbackTest;

Console.WriteLine("Hello SharpGen.Runtime");

// The purpose of this is to prevent `SharpGen.Runtime.Trim.Dummy.CallbackTest` from being linked away.
// as we need to test trimming on the callback.
var unrelated = new UnrelatedClass();