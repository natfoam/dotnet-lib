﻿using System.Reflection;
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
            return info.IsPointer ? 
                new PointerTypeBox(info.GetElementType()!) :
                new NameTypeBox(info.FullName!);
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

        public static string ToCidlString(this IType? type)
            => type switch
            {
                BasicTypeBox x => x.BasicType.ToString(),
                PointerTypeBox p => $"{p.Element.ToCidlString()}*",
                NameTypeBox n => n.Name,
                _ => "void"
            };
    }
}