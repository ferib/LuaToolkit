using System;
using System.Collections.Generic;
using System.Text;

namespace LuaSharpVM
{
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


    public class LuaInstruction
    {
        
        private const int HalfMax18Bit = 2 << 17;	// == 2^18 / 2 == 131071

        public int Data
		{
			get;
			private set;
		}

		public LuaOpcode OpCode
		{
			get;
			private set;
		}

		public int A
		{
			get;
			private set;
		}

		public int B
		{
			get;
			private set;
		}

		public int C
		{
			get;
			private set;
		}

		public int Bx
		{
			get { return ((B << 9) & 0xFFE00 | C) & 0x3FFFF; }
		}

		public int sBx
		{
			get { return Bx - HalfMax18Bit; }
		}

		public bool HasBx
		{
			get;
			private set;
		}

		public bool Signed
		{
			get;
			private set;
		}

		public LuaInstruction(int data)
		{
			Data = data;

			OpCode = (LuaOpcode)(data & 0x3F);
			A = (data >> 6) & 0xFF;
			B = (data >> 23) & 0x1FF;
			C = (data >> 14) & 0x1FF;

			switch (OpCode)
			{
				case LuaOpcode.JMP:
				case LuaOpcode.FORLOOP:
				case LuaOpcode.FORPREP:
					Signed = true;
					goto case LuaOpcode.LOADK;

				case LuaOpcode.LOADK:
				case LuaOpcode.GETGLOBAL:
				case LuaOpcode.SETGLOBAL:
				case LuaOpcode.CLOSE:
					HasBx = true;
					break;

				default:
					HasBx = false;
					Signed = false;
					break;
			}
		}

        public override string ToString()
        {
            // TODO: check if this makes sense
            if(this.Signed && this.HasBx)
                return $"{this.OpCode} {this.sBx} {this.sBx}";
            if(this.Signed)
                return $"{this.OpCode} {this.sBx}";
            if(this.HasBx)
                return $"{this.OpCode} {this.Bx}";
            else
                return $"{this.OpCode} {this.A} {this.B} {this.C}";
        }
    }
    //public static class LuaInstructions
    //{
    //    public static LuaInstruction[] Table =
    //    {
    //        new LuaInstruction { Opcode=LuaOpcode.MOVE, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.LOADK, Type=OpcodeType.ABx },
    //        new LuaInstruction { Opcode=LuaOpcode.LOADBOOL, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.LOADNIL, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.GETUPVAL, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.GETGLOBAL, Type=OpcodeType.ABx },
    //        new LuaInstruction { Opcode=LuaOpcode.GETTABLE, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.SETGLOBAL, Type=OpcodeType.ABx },
    //        new LuaInstruction { Opcode=LuaOpcode.SETUPVAL, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.SETTABLE, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.NEWTABLE, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.SELF, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.ADD, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.SUB, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.MUL, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.DIV, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.MOD, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.POW, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.UNM, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.NOT, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.LEN, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.CONCAT, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.JMP, Type=OpcodeType.AsBx },
    //        new LuaInstruction { Opcode=LuaOpcode.EQ, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.LT, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.LE, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.TEST, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.TESTSET, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.CALL, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.TAILCALL, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.RETURN, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.FORLOOP, Type=OpcodeType.AsBx },
    //        new LuaInstruction { Opcode=LuaOpcode.FORPREP, Type=OpcodeType.AsBx },
    //        new LuaInstruction { Opcode=LuaOpcode.TFORLOOP, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.SETLIST, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.CLOSE, Type=OpcodeType.ABC },
    //        new LuaInstruction { Opcode=LuaOpcode.CLOSURE, Type=OpcodeType.ABx },
    //        new LuaInstruction { Opcode=LuaOpcode.VARARG, Type=OpcodeType.ABC },
    //    };

    //}
}
