using System.Reflection;
using System.Runtime.InteropServices;
using Text;

namespace Cidl
{
    sealed class Library
    {
        public readonly string Name;
        public readonly Dictionary<string, TypeDef> Map;
        public Library(Assembly assembly)
        {
            Name = assembly
                .GetName()
                .Name!;
            Map = assembly.DefinedTypes
                .SelectMany(CidlEx.ToCidlTypeDef)
                .ToDictionary(i => i.Key, i => i.Value);
        }

        public IEnumerable<Item> List()
            => Map
                .SelectMany(kv => kv.Value.List(kv.Key))
                .Block()
                .Curly($"library {Name}"); 
    }

    abstract class TypeDef 
    {
        public abstract IEnumerable<Item> List(string name);
    }

    sealed class Struct: TypeDef
    {
        public readonly Param[] FieldList;

        public Struct(Param[] fieldList)
        {
            FieldList = fieldList;
        }

        public Struct(IEnumerable<FieldInfo> info)
        {
            FieldList = info
                .Select(CidlEx.Param)
                .ToArray();
        }

        public override IEnumerable<Item> List(string name)
            => FieldList
                .Select(p => $"{p.Type.ToCidlString()} {p.Name};".Line())
                .Block()
                .Curly($"struct {name}");
    }

    sealed class Interface : TypeDef
    {
        public readonly Guid Guid;
        public readonly Method[] Methods;

        public Interface(TypeInfo type)
        {
            Guid = type.GUID;
            Methods = type
                .DeclaredMethods
                .Select(CidlEx.Method)
                .ToArray();
        }

        public override IEnumerable<Item> List(string name)
            => $"[Guid({Guid})]"
                .Line()
                .One()
                .Concat(Methods
                    .Select(CidlEx.Line)
                    .Block()
                    .Curly($"interface {name}"));
    }

    sealed class Param
    {
        public readonly TypeRef Type;
        public readonly string Name;
        public Param(ParameterInfo info)
        {
            Type = info.ParameterType
                .ToCidlType();
            Name = info.Name!;
        }

        public Param(FieldInfo info)
        {
            Type = info.FieldType!
                .ToCidlType();
            Name = info.Name;
        }
    }

    sealed class Method
    {
        public readonly string Name;
        public readonly TypeRef? ReturnType;
        public readonly Param[] ParamList;

        public Method(MethodInfo method)
        {
            if (method.CustomAttributes.FirstOrDefault(v => v.AttributeType == typeof(PreserveSigAttribute))
                == null)
            {
                throw new Exception("PreserveSig attribute is required");
            }
            Name = method.Name;
            ReturnType = method
                .ToCidlReturnType();
            ParamList = method
                .GetParameters()
                .Select(CidlEx.Param)
                .ToArray();
        }
    }

    enum BasicType
    {
        I8,
        U8,
        I16,
        U16,
        I32,
        U32,
        I64,
        U64,
        Bool,
    }

    abstract class TypeRef
    {
    }

    sealed class BasicTypeRef : TypeRef
    {
        public readonly BasicType BasicType;

        public BasicTypeRef(BasicType basicType)
        {
            BasicType = basicType;
        }
    }

    sealed class PointerTypeRef : TypeRef
    {
        public readonly TypeRef Element;

        public PointerTypeRef(Type info)
        {
            Element = info.ToCidlType();
        }
    }

    sealed class NameTypeRef : TypeRef
    {
        public readonly string Name;

        public NameTypeRef(string name)
        {
            Name = name;
        }
    }

    static class CidlEx
    {
        public static Param Param(this FieldInfo info)
            => new Param(info);

        public static Param Param(this ParameterInfo info)
            => new Param(info);

        public static Method Method(this MethodInfo method)
            => new Method(method);

        public static Interface Interface(this TypeInfo type)
            => new Interface(type);

        public static Struct Struct(this TypeInfo type)
            => new Struct(type.DeclaredFields);

        public static BasicTypeRef Ref(this BasicType basicType)
            => new BasicTypeRef(basicType);

        public static PointerTypeRef Pointer(this Type type)
            => new PointerTypeRef(type);

        public static NameTypeRef NameTypeRef(this string name)
            => new NameTypeRef(name);

        public static TypeRef? ToCidlReturnType(this MethodInfo info)
            => info.ReturnType switch
            {
                var r when r == typeof(void) => null,
                var r => r
                    .ToCidlType(),
            };

        public static TypeRef ToCidlType(this Type info)
            => info.ToClidBasicType() switch
            {
                null when info.IsPointer => info
                    .GetElementType()!
                    .Pointer(),
                null => info.Name!
                    .NameTypeRef(),
                var b => b.Value
                    .Ref()
            };

        public static BasicType? ToClidBasicType(this Type info)
            => info switch 
            { 
                var i when i == typeof(sbyte) => BasicType.I8,
                var i when i == typeof(byte) => BasicType.U8,
                var i when i == typeof(short) => BasicType.I16,
                var i when i == typeof(ushort) => BasicType.U16,
                var i when i == typeof(int) => BasicType.I32,
                var i when i == typeof(uint) => BasicType.U32,
                var i when i == typeof(long) => BasicType.I64,
                var i when i == typeof(ulong) => BasicType.U64,
                var i when i == typeof(bool) => BasicType.Bool,
                _ => null, 
            };

        public static string ToCidlString(this TypeRef? type)
            => type switch
            {
                BasicTypeRef x => x.BasicType
                    .ToString(),
                PointerTypeRef p => $"{p.Element.ToCidlString()}*",
                NameTypeRef n => n.Name,
                _ => "void"
            };

        static IEnumerable<KeyValuePair<string, TypeDef>> ToPair(this TypeInfo info, TypeDef def)
            => KeyValuePair
                .Create(info.Name!, def)
                .One();

        public static IEnumerable<KeyValuePair<string, TypeDef>> ToCidlTypeDef(this TypeInfo type)
            => type switch
            {
                var t when t.IsInterface => t.ToPair(t.Interface()),
                var t when t.IsValueType && !t.IsEnum && t.IsLayoutSequential => t.ToPair(t.Struct()),
                _ => Enumerable.Empty<KeyValuePair<string, TypeDef>>(),
            };

        public static string String(this IEnumerable<Param> list)
            => list
                .Select(v => $"{v.Type.ToCidlString()} {v.Name}")
                .Join(", ");

        public static Line Line(this Method m)
            => $"{m.ReturnType.ToCidlString()} {m.Name}({m.ParamList.String()});"
                .Line();
    }
}