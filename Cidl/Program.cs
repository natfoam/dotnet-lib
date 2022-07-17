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

abstract class Item 
{
    public abstract IEnumerable<string> Text(string indent, string offset = "");
}

sealed class Line: Item
{
    public readonly string Value;

    public Line(string value)
    {
        Value = value;
    }

    public override IEnumerable<string> Text(string indent, string offset)
        => new[] { offset + Value };
}

sealed class Block: Item
{
    public readonly IEnumerable<Item> ItemList;

    public Block(IEnumerable<Item> itemList)
    {
        ItemList = itemList;
    }

    public override IEnumerable<string> Text(string indent, string offset)
        => ItemList.SelectMany(item => item.Text(indent, offset));
}

static class TextEx
{
    public static void Write(this Item item, string indent)
    {
        foreach (var line in item.Text(indent))
        {
            Console.WriteLine(line);
        }
    }
}
