using Cidl;
using System.Reflection;

var a = Assembly.LoadFile(
    "c:/github.com/natfoam/dotnet-lib/CidlExample/bin/Debug/netstandard2.0/CidlExample.dll");

var library = new Library(a.DefinedTypes);

LibraryItemList(library).Write("  ");

static IEnumerable<Item> LibraryItemList(Library library)
    => Enumerable.Concat(InterfaceMapItemList(library.InterfaceMap), StructMapItemList(library.StructMap));

static IEnumerable<Item> StructMapItemList(Dictionary<string, Struct> map)
    => map.SelectMany(kv => kv.Value.List(kv.Key));


static IEnumerable<Item> InterfaceMapItemList(Dictionary<string, Interface> map)
    => map.SelectMany(kv => kv.Value.List(kv.Key));
