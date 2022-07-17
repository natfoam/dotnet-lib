using Cidl;
using System.Reflection;

var a = Assembly.LoadFile(
    "c:/github.com/natfoam/dotnet-lib/CidlExample/bin/Debug/netstandard2.0/CidlExample.dll");

var library = new Library(a.DefinedTypes);

LibraryItemList(library).Write("  ");

static IEnumerable<Item> LibraryItemList(Library library)
    => new[] { InterfaceMapItemList(library.InterfaceMap), StructMapItemList(library.StructMap) }
        .SelectMany(x => x);

static IEnumerable<Item> StructItemList(KeyValuePair<string, Struct> s)
{
    yield return new Line($"struct {s.Key}");
    yield return new Line("{");
    yield return new Block(s.Value.Params.Select(p => new Line($"{p.Type.ToCidlString()} {p.Name};")));
    yield return new Line("}");
}

static IEnumerable<Item> StructMapItemList(Dictionary<string, Struct> map)
    => map.SelectMany(StructItemList);

static Line MethodStr(Method m)
{
    var p = string.Join(", ", m.Params.Select(v => $"{v.Type.ToCidlString()} {v.Name}"));
    return new Line($"{m.ReturnType.ToCidlString()} {m.Name}({p});");
}

static IEnumerable<Item> InterfaceItemList(KeyValuePair<string, Interface> i)
{
    yield return new Line($"[Guid({i.Value.Guid})]");
    yield return new Line($"interface {i.Key}");
    yield return new Line("{");
    yield return new Block(i.Value.Methods.Select(MethodStr));
    yield return new Line("}");
}

static IEnumerable<Item> InterfaceMapItemList(Dictionary<string, Interface> map)
    => map.SelectMany(InterfaceItemList);

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
    {
        yield return offset + Value;
    }
}

sealed class Block: Item
{
    public readonly IEnumerable<Item> ItemList;

    public Block(IEnumerable<Item> itemList)
    {
        ItemList = itemList;
    }

    public override IEnumerable<string> Text(string indent, string offset)
        => ItemList.SelectMany(item => item.Text(indent, offset + indent));
}

static class TextEx
{
    public static void Write(this IEnumerable<Item> list, string indent)
    {
        foreach (var item in list)
        {
            item.Write(indent);
        }
    }

    public static void Write(this Item item, string indent)
    {
        foreach (var line in item.Text(indent))
        {
            Console.WriteLine(line);
        }
    }
}
