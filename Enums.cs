using System;
using System.Collections.Generic;
using System.Text;

namespace LuaSharpVM
{
    public struct LuaInstruction
    {
        public OpcodeName Opcode;
        public OpcodeType Type; // ABC, ABx, AsBx
        public int A;
        public int B;
        public int Bx;
        public int C;
        public int sBx;
    }

    public struct LuaConstant
    {
        public ConstantType Type;
        public object Data;
    }

    public enum ConstantType
    {
        BOOL = 1,
        FLOAT = 3,
        STRING = 4
    }

    public enum OpcodeName
    {
        MOVE = 0,
        LOADK,
        LOADBOOL,
        LOADNIL,
        GETUPVAL,
        GETGLOBAL,
        GETTABLE,
        SETGLOBAL,
        SETUPVAL,
        SETTABLE,
        NEWTABLE,
        SELF,
        ADD,
        SUB,
        MUL,
        DIV,
        MOD,
        POW,
        UNM,
        NOT,
        LEN,
        CONCAT,
        JMP,
        EQ,
        LT,
        LE,
        TEST,
        TESTSET,
        CALL,
        TAILCALL,
        RETURN,
        FORLOOP,
        FORPREP,
        TFORLOOP,
        SETLIST,
        CLOSE,
        CLOSURE,
        VARARG
    }

    public enum OpcodeType
    {
        ABC,
        ABx,
        AsBx
    }
}
