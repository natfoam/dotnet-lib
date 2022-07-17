using System.Reflection;
using System.Runtime.InteropServices;

var a = Assembly.LoadFile(
    "c:/github.com/natfoam/dotnet-lib/CidlExample/bin/Debug/netstandard2.0/CidlExample.dll");

var interfaces = a.DefinedTypes
    .Where(type => type.IsInterface)
    .Select(type => (type.FullName, new Interface(type)));

var structures = a.DefinedTypes
    .Where(type => type.IsValueType && !type.IsEnum && type.IsLayoutSequential)
    .Select(type => (type.FullName, new Struct(type.DeclaredFields)));

foreach (var (name, i) in interfaces)
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

foreach (var (name, i) in structures)
{
    Console.WriteLine($"struct {name}");
    Console.WriteLine($"{{");
    foreach (var f in i.Params)
    {
        Console.WriteLine($"  {f.Type.ToCidlString()} {f.Name};");
    }
    Console.WriteLine($"}}");
}

class Struct
{
    public readonly Param[] Params;

    public Struct(Param[] @params)
    {
        Params = @params;
    }

    public Struct(IEnumerable<FieldInfo> info)
    {
        Params = info.Select(v => new Param(v)).ToArray();
    }
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
}

class Param
{
    public readonly IType Type;
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
    public readonly IType? ReturnType;
    public readonly Param[] Params;

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
        Params = method.GetParameters().Select(p => new Param(p)).ToArray();
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

interface IType
{
}

class BasicTypeBox : IType
{
    public readonly BasicType BasicType;

    public BasicTypeBox(BasicType basicType)
    {
        BasicType = basicType;
    }
}

class PointerTypeBox : IType
{
    public readonly IType Element;

    public PointerTypeBox(Type info)
    {
        Element = info.ToCidlType();
    }
}

class NameTypeBox : IType
{
    public readonly string Name;

    public NameTypeBox(string name)
    {
        Name = name;
    }
}

static class TypeEx
{
    public static IType? ToCidlReturnType(this MethodInfo info)
    {
        var returnType = info.ReturnType;
        return returnType == typeof(void) ? null : returnType.ToCidlType();
    }

    public static IType ToCidlType(this Type info)
    {
        {
            var basicType = info.ToClidBasicType();
            if (basicType != null) { return new BasicTypeBox(basicType.Value); }
        }
        if (info.IsPointer)
        {
            return new PointerTypeBox(info.GetElementType()!);
        }
        return new NameTypeBox(info.FullName!);
    }

    public static BasicType? ToClidBasicType(this Type info)
    {
        if (info == typeof(sbyte)) { return BasicType.I8; }
        if (info == typeof(byte)) { return BasicType.U8; }
        if (info == typeof(short)) { return BasicType.I16; }
        if (info == typeof(ushort)) { return BasicType.U16; }
        if (info == typeof(int)) { return BasicType.I32; }
        if (info == typeof(uint)) { return BasicType.U32; }
        if (info == typeof(long)) { return BasicType.I64; }
        if (info == typeof(ulong)) { return BasicType.U64; }
        if (info == typeof(bool)) { return BasicType.Bool; }
        return null;
    }

    public static string ToCidlString(this IType? type)
        => type switch
        {
            BasicTypeBox x => x.BasicType.ToString(),
            PointerTypeBox p => $"{p.Element.ToCidlString()}*",
            NameTypeBox n => n.Name,
            _ => "void"
        };
}
