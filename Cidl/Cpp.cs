using Text;

namespace Cidl
{
    static class CppEx
    {
        public static string Cpp(this BasicType type)
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

        public static string Cpp(this TypeRef? type, Library library)
            => type switch
            {
                BasicTypeRef b => b.BasicType.Cpp(),
                PointerTypeRef p => $"{p.Element.Cpp(library)}*",
                NameTypeRef n => library.Map[n.Name] switch
                {
                    Interface i => $"{n.Name}*",
                    _ => n.Name,
                },
                _ => "void",
            };

        public static string Cpp(this IEnumerable<Param> paramList, Library library)
            => string.Join(", ", paramList.Select(p => $"{p.Type.Cpp(library)} {p.Name}"));

        public static Item Cpp(this Method m, Library library)
            => $"virtual {m.ReturnType.Cpp(library)} __stdcall {m.Name}({m.ParamList.Cpp(library)}) = 0;"
                .Line();

        public static IEnumerable<Item> Cpp(this KeyValuePair<string, TypeDef> kv, Library library)
            => kv.Value switch
            {
                Struct s => s.Cpp(library, kv.Key),
                Interface i => i.Cpp(library, kv.Key),
                _ => Enumerable.Empty<Item>(),
            };

        public static IEnumerable<Item> Cpp(this Struct s, Library library, string name)
            => new Block(s.FieldList.Select(f => $"{f.Type.Cpp(library)} {f.Name};".Line())).Curly($"struct {name}");

        public static IEnumerable<Item> Cpp(this Interface i, Library library, string name)
            => new Block(i.Methods.Select(m => m.Cpp(library))).Curly($"struct {name}: IUnknown");

        public static IEnumerable<Item> Cpp(this Dictionary<string, TypeDef> map, Library library)
            => map.Select(kv => $"struct {kv.Key};".Line()).Concat(map.SelectMany(def => def.Cpp(library)));

        public static IEnumerable<Item> Cpp(this Library library)
            => new[] { "#pragma once".Line() }
                .Concat(new Block(library.Map.Cpp(library)).Curly($"namespace {library.Name}"));
    }
}
