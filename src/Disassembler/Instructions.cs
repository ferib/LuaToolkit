using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace LuaToolkit.Disassembler
{
    public class Instruction
    {
        public const int SIZE_OP = 6;
        public const int SIZE_C = 9;
        public const int SIZE_B = SIZE_C;
        public const int SIZE_A = 8;
        public const int SIZE_Bx = SIZE_C + SIZE_B;

        public const int POS_OP = 0;
        public const int POS_A = POS_OP + SIZE_OP;
        public const int POS_C = POS_A + SIZE_A;
        public const int POS_B = POS_C + SIZE_C;
        public const int POS_Bx = POS_C;

        public const int MAX_ARG_A = (1 << SIZE_A) - 1;
        public const int MAX_ARG_B = (1 << SIZE_B) - 1;
        public const int MAX_ARG_C = (1 << SIZE_C) - 1;
        public const int MAX_ARG_Bx = (1 << SIZE_Bx) - 1;
        public const int MAX_ARG_sBx = MAX_ARG_Bx >> 1;

        private uint MASK1(int n, int p)
        {
            return (~((~(uint)0) << n)) << p;
        }

        private uint MASK0(int n, int p)
        {
            return ~MASK1(n, p);
        }

        public Instruction(uint data, int lineNumber)
        {
            Data = data;
            LineNumber = lineNumber;
            Branchers = new List<JmpInstruction>();
        }

        public Instruction(int lineNumber)
        {
            Data = 0;
            LineNumber = lineNumber;
            Branchers = new List<JmpInstruction>();
        }

        public uint Data
        {
            get;
            set;
        }

        public LuaOpcode OpCode
        {
            get
            {
                return (LuaOpcode)((Data >> POS_OP) & MASK1(SIZE_OP, 0));
            }
            set
            {
                Data = (Data & MASK0(SIZE_OP, POS_OP)) | 
                    (((uint)value << POS_OP) & MASK1(SIZE_OP, POS_OP));
            }
        }

        public int A
        {
            get {
                return (int)((Data >> POS_A) & MASK1(SIZE_A, 0));
            }
            set
            {
                Data = (Data & MASK0(SIZE_A, POS_A)) |
                    (((uint)value << POS_A) & MASK1(SIZE_A, POS_A));
            }
        }

        public int B
        {
            get
            {
                return (int)((Data >> POS_B) & MASK1(SIZE_B, 0));
            }
            set
            {
                Data = (Data & MASK0(SIZE_B, POS_B)) |
                    (((uint)value << POS_B) & MASK1(SIZE_B, POS_B));
            }
        }

        public int C
        {
            get
            {
                return (int)((Data >> POS_C) & MASK1(SIZE_C, 0));
            }
            set
            {
                Data = (Data & MASK0(SIZE_C, POS_C)) |
                    (((uint)value << POS_C) & MASK1(SIZE_C, POS_C));
            }
        }

        public int Bx
        {
            get
            {
                return (int)((Data) >> POS_Bx) & (int)MASK1(SIZE_Bx, 0);
            }
            set
            {
                Data = (((Data) & MASK0(SIZE_Bx, POS_Bx)) | 
                    (((uint)value << POS_Bx) & MASK1(SIZE_Bx, POS_Bx)));
            }
        }

        public int sBx
        {
            get { return Bx - MAX_ARG_sBx; }
            set { Bx = value + MAX_ARG_sBx; }
        }

        public OpcodeType OpcodeType
        {
            get;
            set;
        }

        public int LineNumber
        {
            get;
            set;
        }

        virtual public string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(LineNumber).Append(" ");
            sb.Append(OpCode.ToString()).Append(" ");
            switch (OpcodeType)
            {
                case OpcodeType.ABC:
                    sb.Append(A).Append(" ").Append(B).Append(" ").Append(C);
                    break;
                case OpcodeType.ABx:
                    sb.Append(A).Append(" ").Append(Bx);
                    break;
                case OpcodeType.AsBx:
                    sb.Append(A).Append(" ").Append(sBx);
                    break;
                default:
                    Debug.Assert(false, "Unknown OpcodeType");
                    break;
            }
            return sb.ToString();
        }
        
        public Function Function
        {
            get;
            set;
        }

        public Block Parent
        {
            get;
            set;
        }

        public Block Block
        {
            get { return Parent; }
            set { Parent = value; }
        }

        public List<JmpInstruction> Branchers;
    }

    public class MoveInstruction : Instruction
    {
        public MoveInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.MOVE;
            OpcodeType = OpcodeType.ABC;
        }

        public MoveInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpCode = LuaOpcode.MOVE;
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class LoadKInstruction : Instruction
    {
        public LoadKInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.LOADK;
            OpcodeType = OpcodeType.ABx;
        }
        public LoadKInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABx;
        }

        public override string Dump()
        {
            var sb = new StringBuilder();
            sb.Append(base.Dump());
            sb.Append(" -- ").Append(Constant.Dump());
            return sb.ToString();
        }

        public int ConstIndex
        {
            get
            {
                var index = Bx - MAX_ARG_Bx;
                return Math.Abs(index);
            }
            private set { }
        }

        public ByteConstant Constant
        {
            get
            {
                var index = ConstIndex;
                if(index >= Function.Constants.Count)
                {
                    //Debug.Assert(false, "Index " + index +
                    //" is bigger than the number of constants");
                    return new NumberByteConstant(index);
                }
                return Function.Constants[index];
            }
            private set { }
        }
    }

    public class LoadBoolInstruction : Instruction
    {
        public LoadBoolInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.LOADBOOL;
            OpcodeType = OpcodeType.ABC;
        }
        public LoadBoolInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC ;
        }
    }

    public class LoadNilInstruction : Instruction
    {
        public LoadNilInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.LOADNIL;
            OpcodeType = OpcodeType.ABC;
        }
        public LoadNilInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class GetUpvalInstruction : Instruction
    {
        public GetUpvalInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.GETUPVAL;
            OpcodeType = OpcodeType.ABC;
        }
        public GetUpvalInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class SetUpvalInstruction : Instruction
    {
        public SetUpvalInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.SETUPVAL;
            OpcodeType = OpcodeType.ABC;
        }
        public SetUpvalInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class GetGlobalInstruction : Instruction
    {
        public GetGlobalInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.GETGLOBAL;
            OpcodeType = OpcodeType.ABx;
        }
        public GetGlobalInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABx;
        }
    }

    public class SetGlobalInstruction : Instruction
    {
        public SetGlobalInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.SETGLOBAL;
            OpcodeType = OpcodeType.ABx;
        }
        public SetGlobalInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABx;
        }
    }

    public class GetTableInstruction : Instruction
    {
        public GetTableInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.GETTABLE;
            OpcodeType = OpcodeType.ABC;
        }
        public GetTableInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class SetTableInstruction : Instruction
    {
        public SetTableInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.SETTABLE;
            OpcodeType = OpcodeType.ABC;
        }
        public SetTableInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class NewTableInstruction : Instruction
    {
        public NewTableInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.NEWTABLE;
            OpcodeType = OpcodeType.ABC;
        }
        public NewTableInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class SelfInstruction : Instruction
    {
        public SelfInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.SELF;
            OpcodeType = OpcodeType.ABC;
        }
        public SelfInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class AddInstruction : Instruction
    {
        public AddInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.ADD;
            OpcodeType = OpcodeType.ABC;
        }
        public AddInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class SubInstruction : Instruction
    {
        public SubInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.SUB;
            OpcodeType = OpcodeType.ABC;
        }
        public SubInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class MulInstruction : Instruction
    {
        public MulInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.MUL;
            OpcodeType = OpcodeType.ABC;
        }
        public MulInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class DivInstruction : Instruction
    {
        public DivInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.DIV;
            OpcodeType = OpcodeType.ABC;
        }
        public DivInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class ModInstruction : Instruction
    {
        public ModInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.MOD;
            OpcodeType = OpcodeType.ABC;
        }
        public ModInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class PowInstruction : Instruction
    {
        public PowInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.POW;
            OpcodeType = OpcodeType.ABC;
        }
        public PowInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class UnmInstruction : Instruction
    {
        public UnmInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.UNM;
            OpcodeType = OpcodeType.ABC;
        }
        public UnmInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class NotInstruction : Instruction
    {
        public NotInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.NOT;
            OpcodeType = OpcodeType.ABC;
        }
        public NotInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class LenInstruction : Instruction
    {
        public LenInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.LEN;
            OpcodeType = OpcodeType.ABC;
        }
        public LenInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class ConcatInstruction : Instruction
    {
        public ConcatInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.CONCAT;
            OpcodeType = OpcodeType.ABC;
        }
        public ConcatInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class JmpInstruction : Instruction
    {
        public JmpInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.JMP;
            OpcodeType = OpcodeType.AsBx;
        }
        public JmpInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.AsBx;
        }

        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.Dump());
            sb.Append(" -- ").Append(TargetAddress);

            return sb.ToString();
        }

        public int TargetAddress
        {
            get
            {
                return LineNumber + sBx + 1;
            }
            private set
            {
                Debug.Assert(false, "We should never call this");
            }
        }

        public Instruction Target;
    }

    public class EqInstruction : Instruction
    {
        public EqInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.EQ;
            OpcodeType = OpcodeType.ABC;
        }
        public EqInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class LtInstruction : Instruction
    {
        public LtInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.LT;
            OpcodeType = OpcodeType.ABC;
        }
        public LtInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class LeInstruction : Instruction
    {
        public LeInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.LE;
            OpcodeType = OpcodeType.ABC;
        }
        public LeInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class TestInstruction : Instruction
    {
        public TestInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.TEST;
            OpcodeType = OpcodeType.ABC;
        }
        public TestInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class TestSetInstruction : Instruction
    {
        public TestSetInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.TESTSET;
            OpcodeType = OpcodeType.ABC;
        }
        public TestSetInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class CallInstruction : Instruction
    {
        public CallInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.CALL;
            OpcodeType = OpcodeType.ABC;
        }
        public CallInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class TailCallInstruction : Instruction
    {
        public TailCallInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.TAILCALL;
            OpcodeType = OpcodeType.ABC;
        }
        public TailCallInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class ReturnInstruction : Instruction
    {
        public ReturnInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.RETURN;
            OpcodeType = OpcodeType.ABC;
        }
        public ReturnInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class ForLoopInstruction : Instruction
    {
        public ForLoopInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.FORLOOP;
            OpcodeType = OpcodeType.AsBx;
        }
        public ForLoopInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.AsBx;
        }

        public int TargetAddress
        {
            get
            {
                return LineNumber + sBx + 1;
            }
            private set
            {
                Debug.Assert(false, "We should never call this");
            }
        }

        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.Dump());
            sb.Append(" -- ").Append(TargetAddress);

            return sb.ToString();
        }

        public ForPrepInstruction ForPrep;
        public Instruction Target;
    }

    public class ForPrepInstruction : Instruction
    {
        public ForPrepInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.FORPREP;
            OpcodeType = OpcodeType.AsBx;
        }
        public ForPrepInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.AsBx;
        }
        public int TargetAddress
        {
            get
            {
                return LineNumber + sBx + 1;
            }
            private set
            {
                Debug.Assert(false, "We should never call this");
            }
        }

        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(base.Dump());
            sb.Append(" -- ").Append(TargetAddress);

            return sb.ToString();
        }

        public ForLoopInstruction ForLoop;
    }

    public class TForLoopInstruction : Instruction
    {
        public TForLoopInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.TFORLOOP;
            OpcodeType = OpcodeType.ABC;
        }
        public TForLoopInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class SetListInstruction : Instruction
    {
        public SetListInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.SETLIST;
            OpcodeType = OpcodeType.ABC;
        }
        public SetListInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class CloseInstruction : Instruction
    {
        public CloseInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.CLOSE;
            OpcodeType = OpcodeType.ABC;
        }
        public CloseInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }

    public class ClosureInstruction : Instruction
    {
        public ClosureInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.CLOSURE;
            OpcodeType = OpcodeType.ABx;
        }
        public ClosureInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABx;
        }
    }

    public class VarArgInstruction : Instruction
    {
        public VarArgInstruction(int lineNumber) : base(lineNumber)
        {
            OpCode = LuaOpcode.VARARG;
            OpcodeType = OpcodeType.ABC;
        }
        public VarArgInstruction(uint data, int lineNumber) : base(data, lineNumber)
        {
            OpcodeType = OpcodeType.ABC;
        }
    }
}
