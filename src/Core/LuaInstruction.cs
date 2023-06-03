using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace LuaToolkit
{
    public class LuaInstruction
    {
        private const int HalfMax18Bit = 2 << 16;	// == 2^16 -1 == 131071

        public uint Data
        {
            get;
            private set;
        }

        public LuaOpcode OpCode
        {
            get;
            private set;
        }

        private uint MASK1(int n, int p)
        {
            return ((~((~(uint)0) << n)) << p);
        }

        private uint MASK0(int n, int p)
        {
            return (~MASK1(n, p));
        }

        public int A
        {
            get { return (int)(Data >> 6) & 0xFF; }
            set
            {
                //_A = value;
                //Data = ((Data & ~0xFF) | ((value >> 6) & 0xFF));
                Data = (uint)((Data & ~0x00003FC0) | (((uint)value & 0xFF) << 6) );
            }
        }

        public int B
        {
            get { return (int)(Data >> 23) & 0x1FF; }
            set
            {
                //_B = value;
                //Data = ((Data & ~0x1FF) | ((value >> 23) & 0x1FF));
                //UpdateData();
                //Data = ((Data & ~0xFF800000)   | ((value >> 23) & 0x1FF));
                Data = (uint)((Data & ~0xFF800000) | (((uint)value & 0x1FF) << 23));
            }
        }

        public int C
        {
            get { return (int)(Data >> 14) & 0x1FF; }
            set
            {
                //_C = value;
                //Data = ((Data & ~0x1FF) | ((value >> 14) & 0x1FF));
                //UpdateData();
                Data = (uint)((Data & ~0x007FC000) | (((uint)value & 0x1FF) << 14));
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        struct IntConverter
        {
            [FieldOffset(0)]
            public short Short;
            [FieldOffset(0)]
            public ushort Ushort;
            [FieldOffset(0)]
            public int Int;
            [FieldOffset(0)]
            public uint Uint;
        }


        public int Bx
        {
            //get { return ((B << 9) & 0x000FFE00 | C) & 0x3FFFF; }
          //   get { return ((B << 9) & 0xFFE00 | C) & 0x3FFFF; }
            get
            {
                uint temp = (uint)((Data) >> 14) & (uint)MASK1(18, 0);
                var conv = new IntConverter { Uint = temp };
                return conv.Int;
                // return (int)((Data >> /* POS_A */ 6 + /* SIZE_A */ 8) & ((~((~(uint)0) << /* SIXE_B */ 18)) << 0));
            }
            set 
            {
                Data = (((Data) & MASK0(18, 14)) | (((uint)value << 14) & MASK1(18, 14)));

                //Data = ((Data) & ~((~((~(uint)0) << 18)) << 14)) |
                //    (((uint)(value) << 14) & ((~((~(uint)0) << /* SIXE_B */ 18)) << 14));
                // int b = (value >> 9); // TODO: verift that this gets rid of the first 9 bits?
                // int c = (value & ~0xFFE00); // TODO: verify that this gets rid of the last 9 bits?
                //UpdateData();
                //Data = ((Data & ~0xFF800000)   | ((b >> 23) & 0x1FF));
                // B = b;
                // C = c;
                
                // TODO?
                //Data = (uint)((Data & ~0x007FC000) | ((c & 0x1FF) << 14));
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

        public LuaInstruction(uint data)
        {
            Data = data;

            OpCode = (LuaOpcode)(data & 0x3F);
            //_A = (data >> 6) & 0xFF;
            //_B = (data >> 23) & 0x1FF;
            //_C = (data >> 14) & 0x1FF;

            SetVars();
        }

        public LuaInstruction(LuaOpcode opcode)
        {
            this.OpCode = opcode;
            Data = (uint)((Data & ~0x3F) | ((uint)opcode & 0x3F));
            SetVars();
        }

        //private void UpdateData()
        //{
        //    Data =  ((Data & ~0x3F) | ((int)OpCode & 0x3F ));
        //    A =     ((Data & ~0xFF )   | ((A >> 6)    & 0xFF ));
        //    B =     ((Data & ~0x1FF)   | ((B >> 23)   & 0x1FF));
        //    C =     ((Data & ~0x1FF)   | ((C >> 14)   & 0x1FF));
        //}

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
