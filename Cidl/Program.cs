using Cidl;
using System.Reflection;
using Text;

var path = args[0];
Console.WriteLine(path);
var fullPath = Path.GetFullPath(path);
Console.WriteLine(fullPath);

var a = Assembly.LoadFile(fullPath);
var library = new Library(a);
library.List().Write("  ");

Console.WriteLine();

CppLibrary(library).Write("  ");

static IEnumerable<Item> CppLibrary(Library library)
    => new Block(library.Map.Select(kv => new Line($"struct {kv.Key};"))
        .Concat(library.Map.SelectMany(def => CppTypeDef(library, def))))
    .Curly($"namespace {library.Name}");


static IEnumerable<Item> CppStruct(Library library, Struct s, string name)
    => new Block(s.FieldList.Select(f => new Line($"{CppTypeRef(library, f.Type)} {f.Name};"))).Curly($"struct {name}");

static IEnumerable<Item> CppInterface(Library library, Interface i, string name)
    => new Block(i.Methods.Select(m => CppMethod(library, m))).Curly($"struct {name}: IUnknown");

static IEnumerable<Item> CppTypeDef(Library library, KeyValuePair<string, TypeDef> kv)
    => kv.Value switch
    {
        Struct s => CppStruct(library, s, kv.Key),
        Interface i => CppInterface(library, i, kv.Key),
        _ => Enumerable.Empty<Item>(),
    };

static Item CppMethod(Library library, Method m)
    => new Line($"virtual {CppTypeRef(library, m.ReturnType)} __stdcall {m.Name}({CppParamList(library, m.ParamList)}) = 0;");

static string CppParamList(Library library, IEnumerable<Param> paramList)
    => string.Join(", ", paramList.Select(p => $"{CppTypeRef(library, p.Type)} {p.Name}"));

static string CppBasicType(BasicType type)
    => type switch
    {
        BasicType.I8 => "int8_t",
        BasicType.U8 => "uint8_t",
        BasicType.I16 => "int16_t",
        BasicType.U16 => "uint16_t",
        BasicType.I32 => "int32_t",
        BasicType.U32 => "uint32_t",
        BasicType.I64 => "int64_t",
        BasicType.U64 => "uint64_t",
        BasicType.Bool => "BOOL",
        _ => "void",
    };

static string CppTypeRef(Library library, TypeRef? type)
    => type switch
    {
        BasicTypeRef b => CppBasicType(b.BasicType),
        PointerTypeRef p => $"{CppTypeRef(library, p.Element)}*",
        NameTypeRef n => library.Map[n.Name] switch
        {
            Interface i => $"{n.Name}*",
            _ => n.Name,
        },
        _ => "void",
    };
