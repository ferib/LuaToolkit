using System;
using System.Collections.Generic;
using System.Text;

namespace LuaSharpVM
{
    public struct LuaInstructionOLD
    {
        public LuaOpcode Opcode;
        public OpcodeType Type; // ABC, ABx, AsBx
        public int A;
        public int B;
        public int Bx;
        public int C;
        public int sBx;
    }

    //public struct LuaConstant
    //{
    //    public ConstantType Type;
    //    public object Data;
    //}

    //public enum ConstantType
    //{
    //    NIL = 0,
    //    BOOL = 1,
    //    NUMBER = 3,
    //    STRING = 4
    //}

    public enum VarArg
    {
        Has = 1,
        Is = 2,
        Needs = 4,
    }

    public enum OpcodeType
    {
        ABC,
        ABx,
        AsBx
    }
}
