using System;
using System.Collections.Generic;
using System.Text;

namespace LuaSharpVM
{
    //public enum OpcodeTypes 
    //{
    //    ABC,
    //    ABx,
    //    ABC,
    //    ABC,
    //    ABC,
    //    ABx,
    //    ABC,
    //    ABx,
    //    ABC,
    //    ABX,

    //}

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

    public struct LuaInstruction
    {
        public OpcodeType Opcode;
        public int Type; // ABC, ABx, AsBx
        public int A;
        public int B;
        public int Bx;
        public int C;
        public int sBx;
    }

    public class LuaRegisters
    {
        public byte A;  // 8 bits
        public ushort B;  // 9 bits
        public ushort C;  // 9 buts
        public int Ax  // 26 nits (A, B and C)
        {
            get { return A + B + C; }
            //set { A = value; }
        }
        public int Bx;  // 18 bits (B and C)
        public int sBx // signed Bx
        {
            get { return (short)Bx; }
            set { Bx = value; }
        }
    }

}
