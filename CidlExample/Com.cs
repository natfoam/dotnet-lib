﻿using System.Runtime.InteropServices;

namespace CidlExample
{
    public struct S
    {
        public int A;
        public int B;
        public IMy C;
    }

    public interface IMy
    {
        [PreserveSig]
        void A();
        [PreserveSig]
        unsafe int B(byte x, ushort* p);
        [PreserveSig]
        bool SetS(S s);
    }
}
