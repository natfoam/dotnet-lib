using Cidl;
using System.Reflection;

var a = Assembly.LoadFile(
    "c:/github.com/natfoam/dotnet-lib/CidlExample/bin/Debug/netstandard2.0/CidlExample.dll");

var library = new Library(a.DefinedTypes);

foreach (var (name, i) in library.InterfaceMap)
{
    Console.WriteLine($"[Guid({i.Guid})]");
    Console.WriteLine($"interface {name}");
    Console.WriteLine($"{{");
    foreach (var m in i.Methods)
    {
        var p = string.Join(", ", m.Params.Select(v => $"{v.Type.ToCidlString()} {v.Name}"));
        Console.WriteLine($"  {m.ReturnType.ToCidlString()} {m.Name}({p});");
    }
    Console.WriteLine($"}}");
}

foreach (var (name, i) in library.StructMap)
{
    Console.WriteLine($"struct {name}");
    Console.WriteLine($"{{");
    foreach (var f in i.Params)
    {
        Console.WriteLine($"  {f.Type.ToCidlString()} {f.Name};");
    }
    Console.WriteLine($"}}");
}
