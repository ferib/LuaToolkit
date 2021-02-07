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

    public enum Opcode
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
}
