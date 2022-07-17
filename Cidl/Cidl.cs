using System.Reflection;
using System.Runtime.InteropServices;

namespace Cidl
{
    class Library
    {
        public readonly Dictionary<string, Struct> StructMap;
        public readonly Dictionary<string, Interface> InterfaceMap;
        public Library(IEnumerable<TypeInfo> info)
        {
            InterfaceMap = info
                .Where(type => type.IsInterface)
                .ToDictionary(type => type.FullName!, type => new Interface(type));

            StructMap = info
                .Where(type => type.IsValueType && !type.IsEnum && type.IsLayoutSequential)
                .ToDictionary(type => type.FullName!, type => new Struct(type.DeclaredFields));
        }

        //static IEnumerable<Item> List()
        //    => Enumerable.Concat(InterfaceMapItemList(InterfaceMap), StructMapItemList(StructMap));
    }

    class Struct
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

        public Block Block()
            => new Block(FieldList.Select(p => new Line($"{p.Type.ToCidlString()} {p.Name};")));
    }

    class Interface
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

        public Block Block()
           => new Block(Methods.Select(m => m.Line()));
    }

    class Param
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
            Type = info.DeclaringType!.ToCidlType();
            Name = info.Name;
        }
    }

    class Method
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
                new NameTypeRef(info.FullName!);
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

        public static IEnumerable<Item> List(this KeyValuePair<string, Struct> kv)
        {
            yield return new Line($"struct {kv.Key}");
            yield return new Line("{");
            yield return kv.Value.Block();
            yield return new Line("}");
        }

        public static IEnumerable<Item> List(this KeyValuePair<string, Interface> kv)
        {
            yield return new Line($"[Guid({kv.Value.Guid})]");
            yield return new Line($"interface {kv.Key}");
            yield return new Line("{");
            yield return kv.Value.Block();
            yield return new Line("}");
        }
    }
}