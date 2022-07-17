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
    yield return s.Value.Block();
    yield return new Line("}");
}

static IEnumerable<Item> StructMapItemList(Dictionary<string, Struct> map)
    => map.SelectMany(StructItemList);

static IEnumerable<Item> InterfaceItemList(KeyValuePair<string, Interface> i)
{
    yield return new Line($"[Guid({i.Value.Guid})]");
    yield return new Line($"interface {i.Key}");
    yield return new Line("{");
    yield return i.Value.Block();
    yield return new Line("}");
}

static IEnumerable<Item> InterfaceMapItemList(Dictionary<string, Interface> map)
    => map.SelectMany(InterfaceItemList);
