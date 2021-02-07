using System;
using System.Collections.Generic;
using System.Text;

namespace LuaSharpVM
{
    public static class LuaInstructions
    {
        public static LuaInstruction[] Table =
        {
            new LuaInstruction { Opcode=OpcodeName.MOVE, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.LOADK, Type=OpcodeType.ABx },
            new LuaInstruction { Opcode=OpcodeName.LOADBOOL, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.LOADNIL, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.GETUPVAL, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.GETGLOBAL, Type=OpcodeType.ABx },
            new LuaInstruction { Opcode=OpcodeName.GETTABLE, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.SETGLOBAL, Type=OpcodeType.ABx },
            new LuaInstruction { Opcode=OpcodeName.SETUPVAL, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.SETTABLE, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.NEWTABLE, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.SELF, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.ADD, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.SUB, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.MUL, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.DIV, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.MOD, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.POW, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.UNM, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.NOT, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.LEN, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.CONCAT, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.JMP, Type=OpcodeType.AsBx },
            new LuaInstruction { Opcode=OpcodeName.EQ, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.LT, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.LE, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.TEST, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.TESTSET, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.CALL, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.TAILCALL, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.RETURN, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.FORLOOP, Type=OpcodeType.AsBx },
            new LuaInstruction { Opcode=OpcodeName.FORPREP, Type=OpcodeType.AsBx },
            new LuaInstruction { Opcode=OpcodeName.TFORLOOP, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.SETLIST, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.CLOSE, Type=OpcodeType.ABC },
            new LuaInstruction { Opcode=OpcodeName.CLOSURE, Type=OpcodeType.ABx },
            new LuaInstruction { Opcode=OpcodeName.VARARG, Type=OpcodeType.ABC },
        };

    }
}
