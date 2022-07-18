using Cidl;
using System.Reflection;
using Text;

var a = Assembly.LoadFile(
    "c:/github.com/natfoam/dotnet-lib/CidlExample/bin/Debug/netstandard2.0/CidlExample.dll");

var library = new Library(a.DefinedTypes);
library.List().Write("  ");

Console.WriteLine();

library.Map.Select(kv => new Line($"struct {kv.Key};")).Write("  ");

library.Map.SelectMany(CppTypeDef).Write("  ");

static IEnumerable<Item> CppStruct(Struct s, string name)
    => new Block(s.FieldList.Select(f => new Line(f.Name))).Curly($"struct {name}");

static IEnumerable<Item> CppInterface(Interface i, string name)
    => new Block(i.Methods.Select(CppMethod)).Curly($"struct {name}: IUnknown");

static IEnumerable<Item> CppTypeDef(KeyValuePair<string, TypeDef> kv)
    => kv.Value switch
    {
        Struct s => CppStruct(s, kv.Key),
        Interface i => CppInterface(i, kv.Key),
        _ => Enumerable.Empty<Item>(),
    };

static Item CppMethod(Method m)
    => new Line($"virtual {m.ReturnType.ToCidlString()} __stdcall {m.Name}() = 0;");