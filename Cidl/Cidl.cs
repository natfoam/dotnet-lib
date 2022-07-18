using System.Reflection;
using System.Runtime.InteropServices;
using Text;

namespace Cidl
{
    sealed class Library
    {
        public readonly Dictionary<string, TypeDef> Map;
        public Library(IEnumerable<TypeInfo> info)
        {
            Map = info.SelectMany(TypeEx.ToCidlTypeDef).ToDictionary(i => i.Key, i => i.Value);
        }

        public IEnumerable<Item> List()
            => Map.SelectMany(kv => kv.Value.List(kv.Key)); 
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
            FieldList = info.Select(v => new Param(v)).ToArray();
        }

        public override IEnumerable<Item> List(string name)
            => new Block(FieldList.Select(p => new Line($"{p.Type.ToCidlString()} {p.Name};")))
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
                .Select(method => new Method(method))
                .ToArray();
        }

        public override IEnumerable<Item> List(string name)
            => new[] { new Line($"[Guid({Guid})]") }
                .Concat(new Block(Methods.Select(m => m.Line())).Curly($"interface {name}"));
    }

    sealed class Param
    {
        public readonly TypeRef Type;
        public readonly string Name;
        public Param(ParameterInfo info)
        {
            Type = info.ParameterType.ToCidlType();
            Name = info.Name!;
        }

        public Param(FieldInfo info)
        {
            Type = info.FieldType!.ToCidlType();
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
            if (method.CustomAttributes.FirstOrDefault(v =>
                    v.AttributeType == typeof(PreserveSigAttribute))
                == null)
            {
                throw new Exception("PreserveSig attribute is required");
            }
            Name = method.Name;
            ReturnType = method.ToCidlReturnType();
            ParamList = method.GetParameters().Select(p => new Param(p)).ToArray();
        }

        public Line Line()
        {
            var p = string.Join(", ", ParamList.Select(v => $"{v.Type.ToCidlString()} {v.Name}"));
            return new Line($"{ReturnType.ToCidlString()} {Name}({p});");
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

    static class TypeEx
    {
        public static TypeRef? ToCidlReturnType(this MethodInfo info)
        {
            var returnType = info.ReturnType;
            return returnType == typeof(void) ? null : returnType.ToCidlType();
        }

        public static TypeRef ToCidlType(this Type info)
        {
            {
                var basicType = info.ToClidBasicType();
                if (basicType != null) { return new BasicTypeRef(basicType.Value); }
            }
            return info.IsPointer ? 
                new PointerTypeRef(info.GetElementType()!) :
                new NameTypeRef(info.Name!);
        }

        public static BasicType? ToClidBasicType(this Type info)
            => info switch 
            { 
                _ when info == typeof(sbyte) => BasicType.I8,
                _ when info == typeof(byte) => BasicType.U8,
                _ when info == typeof(short) => BasicType.I16,
                _ when info == typeof(ushort) => BasicType.U16,
                _ when info == typeof(int) => BasicType.I32,
                _ when info == typeof(uint) => BasicType.U32,
                _ when info == typeof(long) => BasicType.I64,
                _ when info == typeof(ulong) => BasicType.U64,
                _ when info == typeof(bool) => BasicType.Bool,
                _ => null, 
            };

        public static string ToCidlString(this TypeRef? type)
            => type switch
            {
                BasicTypeRef x => x.BasicType.ToString(),
                PointerTypeRef p => $"{p.Element.ToCidlString()}*",
                NameTypeRef n => n.Name,
                _ => "void"
            };

        static IEnumerable<KeyValuePair<string, TypeDef>> ToPair(this TypeInfo info, TypeDef def)
            => new[] { KeyValuePair.Create(info.Name!, def) };

        public static IEnumerable<KeyValuePair<string, TypeDef>> ToCidlTypeDef(this TypeInfo type)
            => type switch
            {
                _ when type.IsInterface => type.ToPair(new Interface(type)),
                _ when type.IsValueType && !type.IsEnum && type.IsLayoutSequential
                    => type.ToPair(new Struct(type.DeclaredFields)),
                _ => Enumerable.Empty<KeyValuePair<string, TypeDef>>(),
            };
    }
}