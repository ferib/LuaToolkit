using LuaToolkit.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace LuaToolkit.Core
{
    public class LuaInstruction
    {
        private const int HalfMax18Bit = 2 << 16;	// == 2^16 -1 == 131071

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
            get { return _A; }
            set
            {
                _A = value;
                UpdateData();
            }
        }
        private int _A; 
        
        public int B
        {
            get { return _B; }
            set
            {
                _B = value;
                UpdateData();
            }
        }
        private int _B;

        public int C
        {
            get { return _C; }
            set
            {
                _C = value;
                UpdateData();
            }
        }
        private int _C;

        public int Bx
        {
            get { return ((B << 9) & 0xFFE00 | C) & 0x3FFFF; }
            set 
            { 
                B = value >> 9; // TODO: verift that this gets rid of the first 9 bits?
                C = (value & ~0xFFE00); // TODO: verify that this gets rid of the last 9 bits?
                UpdateData();
            } 
        }

        public int sBx
        {
            get { return Bx - (HalfMax18Bit-1); }
            set { Bx = value + (HalfMax18Bit - 1); }
        }

        private bool _HasBx;

        private bool Signed;


        public string Text
        {
            get { return ToString(); }
        }

        public List<int> OffsetVariables(int offset)
        {
            List<int> originals = new List<int>();

            // offsets the variables (if any)
            bool NoC = false;
            bool NoB = false;
            bool NoA = false;

            // TODO: Complete list
            switch (this.OpCode)
            {
                case LuaOpcode.JMP:
                case LuaOpcode.CLOSE:
                case LuaOpcode.GETUPVAL:
                case LuaOpcode.SETUPVAL:
                    NoA = true;
                    break;
            }
            switch (this.OpCode)
            {
                case LuaOpcode.JMP:
                case LuaOpcode.CLOSE:
                case LuaOpcode.GETUPVAL:
                case LuaOpcode.SETUPVAL:
                case LuaOpcode.CALL:
                case LuaOpcode.TAILCALL:
                    NoB = true;
                    break;
            }
            switch (this.OpCode)
            {
                case LuaOpcode.MOVE:
                case LuaOpcode.LOADNIL:
                case LuaOpcode.GETUPVAL:
                case LuaOpcode.SETUPVAL:
                case LuaOpcode.UNM:
                case LuaOpcode.NOT:
                case LuaOpcode.LEN:
                case LuaOpcode.RETURN:
                case LuaOpcode.VARARG:
                case LuaOpcode.CALL:
                case LuaOpcode.TAILCALL:
                    NoC = true;
                    break;
            }

            // TODO: check if value is NOT constant
            if (!NoA && this.A < 256)
            {
                originals.Add(this.A);
                this.A += offset;
            }
            if (!NoB && this.B < 256)
            {
                this.B += offset;
                originals.Add(this.B);
            }
                
            if (!NoC && this.C < 256)
            {
                this.C += offset;
                originals.Add(this.C);
            }
            return originals;
        }

        public LuaInstruction(int data)
        {
            Data = data;

            OpCode = (LuaOpcode)(data & 0x3F);
            A = (data >> 6) & 0xFF;
            B = (data >> 23) & 0x1FF;
            C = (data >> 14) & 0x1FF;

            SetVars();
        }

        public LuaInstruction(LuaOpcode opcode)
        {
            this.OpCode = opcode;
            Data = ((Data & ~0x3F) | ((int)opcode & 0x3F));
            SetVars();
        }

        private void UpdateData()
        {
            Data =  ((Data & ~0x3F) | ((int)this.OpCode & 0x3F ));
            _A =     ((_A & ~0xFF )   | ((this._A >> 6)    & 0xFF ));
            _B =     ((_B & ~0x1FF)   | ((this._B >> 23)   & 0x1FF));
            _C =     ((_C & ~0x1FF)   | ((this._C >> 14)   & 0x1FF));
        }

        public bool HasBx()
        {
            return this._HasBx;
        }

        public bool IsSigned()
        {
            return this.Signed;
        }

        private void SetVars()
        {
            switch (this.OpCode)
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
                    _HasBx = true;
                    break;

                default:
                    _HasBx = false;
                    Signed = false;
                    break;
            }
        }

        public override string ToString()
        {
            // TODO: cleanup
            if (this.OpCode == LuaOpcode.CLOSE)
                return $"{this.OpCode} {this.A}";
            else if (this.OpCode == LuaOpcode.CLOSURE || this.OpCode == LuaOpcode.GETGLOBAL 
                || this.OpCode == LuaOpcode.SETGLOBAL || this.OpCode == LuaOpcode.LOADK)
                return $"{this.OpCode} {this.A} {this.Bx}";

            if(this.OpCode == LuaOpcode.TFORLOOP || this.OpCode == LuaOpcode.TEST)
                return $"{this.OpCode} {this.A} {this.C}";

            if (this.OpCode == LuaOpcode.JMP)
                return $"{this.OpCode} {this.sBx}";
            else if (this.Signed)
                return $"{this.OpCode} {this.A} {this.sBx}";

            bool NoC = false;
            switch(this.OpCode)
            {
                case LuaOpcode.MOVE:
                case LuaOpcode.LOADNIL:
                case LuaOpcode.GETUPVAL:
                case LuaOpcode.SETUPVAL:
                case LuaOpcode.UNM:
                case LuaOpcode.NOT:
                case LuaOpcode.LEN:
                case LuaOpcode.RETURN:
                case LuaOpcode.VARARG:
                    NoC = true;
                    break;
            }

            if (this._HasBx)
                return $"{this.OpCode} {this.A} {this.Bx}";
            else
                if(NoC)
                    return $"{this.OpCode} {this.A} {this.B}";
                else
                    return $"{this.OpCode} {this.A} {this.B} {this.C}";
        }
    }

    
    
    // NOTE: We can re-use this to calculate the performance impact when we add
    //       some kind of weight to each instruction and do the math.



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
