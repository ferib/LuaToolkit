namespace LuaSharpVM.Models
{
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

    public enum LuaType
    {
        Nil = 0,
        Bool = 1,
        Number = 3,
        String = 4,
    }

    public enum LuaOpcode
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

    public enum LuaOperands
    {

    }
}
