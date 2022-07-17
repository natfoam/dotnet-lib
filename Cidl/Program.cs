using Cidl;
using System.Reflection;

var a = Assembly.LoadFile(
    "c:/github.com/natfoam/dotnet-lib/CidlExample/bin/Debug/netstandard2.0/CidlExample.dll");

new Library(a.DefinedTypes).List().Write("  ");
