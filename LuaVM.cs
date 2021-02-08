using System;
using System.Collections.Generic;

namespace LuaSharpVM
{
    public class LuaFunction
    {
        public string Name;
        public int FirstLineNr;
        public int LastLineNr;
        public byte UpvaluesCount;
        public byte ArgsCount;
        public VarArg Vargs;
        public byte MaxStackSize;
        public List<LuaInstruction> Instructions;
        public List<LuaConstant> Constants;
        public List<LuaFunction> Prototypes;
        public List<int> DebugLines;
        public List<LuaLocal> DebugLocals;
        public List<string> DebugUpvalues;

        public LuaFunction()
        {
            // NOTE: remove?
            this.Instructions = new List<LuaInstruction>();
            this.Constants = new List<LuaConstant>();
            this.Prototypes = new List<LuaFunction>();
            this.DebugLines = new List<int>();
        }
    }

    public class LuaVM
    {
        private bool BigEndian;
        private int IntSize;
        private int SizeT;
        private int Index;
        private byte[] Buffer;
        private LuaFunction Chunk;
        private LuaRegisters Registers;
        private Dictionary<int, object> Stack;
        private new List<LuaConstant> Constants;
        private Dictionary<int, object> Upvalues;
        private Dictionary<int, object> Environment;
        private Dictionary<LuaOpcode, Action> InstructionTable;


        public LuaVM(byte[] LuaC)
        {
            this.Index = 0;
            this.Buffer = LuaC;
            this.Chunk = new LuaFunction();
            this.Registers = new LuaRegisters();
            this.Stack = new Dictionary<int, object>();
            this.Constants = new List<LuaConstant>();
            this.Upvalues = new Dictionary<int, object>();
            this.Environment = new Dictionary<int, object>();
            this.InstructionTable = new Dictionary<LuaOpcode, Action>()
            {
                {LuaOpcode.MOVE, () => {MOVE(); } },
                {LuaOpcode.LOADK, () => {LOADK(); } },
                {LuaOpcode.LOADBOOL, () => {LOADBOOL(); } },
                {LuaOpcode.LOADNIL, () => {LOADNIL(); } },
                {LuaOpcode.GETUPVAL, () => {GETUPVAL(); } },
                {LuaOpcode.GETGLOBAL, () => {GETGLOBAL(); } },
                {LuaOpcode.GETTABLE, () => {GETTABLE(); } },
                {LuaOpcode.SETGLOBAL, () => {SETGLOBAL(); } },
                {LuaOpcode.SETUPVAL, () => {SETUPVAL(); } },
                {LuaOpcode.SETTABLE, () => {SETTABLE(); } },
                {LuaOpcode.NEWTABLE, () => {NEWTABLE(); } },
                {LuaOpcode.SELF, () => {SELF(); } },
                {LuaOpcode.ADD, () => {ADD(); } },
                {LuaOpcode.SUB, () => {SUB(); } },
                {LuaOpcode.MUL, () => {MUL(); } },
                {LuaOpcode.DIV, () => {DIV(); } },
                {LuaOpcode.MOD, () => {MOD(); } },
                {LuaOpcode.POW, () => {POW(); } },
                {LuaOpcode.UNM, () => {UNM(); } },
                {LuaOpcode.NOT, () => {NOT(); } },
                {LuaOpcode.LEN, () => {LEN(); } },
                {LuaOpcode.CONCAT, () => {CONCAT(); } },
                {LuaOpcode.JMP, () => {JUMP(); } },
                {LuaOpcode.EQ, () => {EQ(); } },
                {LuaOpcode.LT, () => {LT(); } },
                {LuaOpcode.LE, () => {LE(); } },
                {LuaOpcode.TEST, () => {TEST(); } },
                {LuaOpcode.TESTSET, () => {TESTSET(); } },
                {LuaOpcode.CALL, () => {CALL(); } },
                {LuaOpcode.TAILCALL, () => {TAILCALL(); } },
                {LuaOpcode.RETURN, () => {RETURN(); } },
                {LuaOpcode.FORLOOP, () => {FORLOOP(); } },
                {LuaOpcode.FORPREP, () => {FORPREP(); } },
                {LuaOpcode.TFORLOOP, () => {TFORLOOP(); } },
                {LuaOpcode.SETLIST, () => {SETLIST(); } },
                {LuaOpcode.CLOSE, () => {CLOSE(); } },
                {LuaOpcode.CLOSURE, () => {CLOSURE(); } },
                {LuaOpcode.VARARG, () => {VARARG(); } },
            };

            if(Verify())
                Decode(); // init the Lua stuff
        }

        public void Execute()
        {
            List<int> localStack = new List<int>();
            List<int> ghostStack = new List<int>();

            loop(); // execute the bytecode
        }

        // decode the metadata and what not from the LuaC file
        private LuaFunction Decode()
        {
            LuaFunction Function = new LuaFunction();

            int count = 0;
            Function.Name = GetString();     // Function name
            Function.FirstLineNr = GetInt();   // First line // 4 or 8?
            Function.LastLineNr = GetInt();    // Last line // 4 or 8?

            // TODO: skip first 2 bytes of this.Chunk.Name
            if (Function.Name != "")
                Function.Name = Function.Name.Substring(0, Function.Name.Length-1);

            // point around
            Function.UpvaluesCount = GetByte();
            Function.ArgsCount = GetByte();
            Function.Vargs = (VarArg)GetByte();
            Function.MaxStackSize = GetByte();

            // Decode Instructions
            Function.Instructions = ReadInstructions();

            // Decode constants
            Function.Constants = ReadConstants();

            // Decode prototypes
            Function.Prototypes = ReadPrototypes();

            // Decode debuginfo: Line Numbers
            Function.DebugLines = ReadDebugLines();

            // Decode debuginfo: Locals
            Function.DebugLocals = ReadDebugLocals();
            
            // Decode debuginfo: Upvalues
            Function.DebugUpvalues = ReadDebugUpvalues();

            return Function;
        }

        private List<LuaInstruction> ReadInstructions()
        {
            List<LuaInstruction> Instructions = new List<LuaInstruction>();
            int count = GetInt(); // 4 or 8?
            for (int i = 0; i < count; i++)
            {
                LuaInstruction instr = new LuaInstruction(GetInt());
                Instructions.Add(instr);
            }
            return Instructions;
        }

        private List<LuaConstant> ReadConstants()
        {
            List<LuaConstant> Constants = new List<LuaConstant>();
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                byte type = GetByte();

                switch ((LuaType)type)
                {
                    case LuaType.Nil:
                        Constants.Add(new NilConstant());
                        break;
                    case LuaType.Bool:
                        Constants.Add(new BoolConstant(GetByte() != 0));
                        break;
                    case LuaType.Number:
                        Constants.Add(new NumberConstant(GetInt()));
                        break;
                    case LuaType.String:
                        Constants.Add(new StringConstant(GetString()));
                        break;
                }
            }
            return Constants;
        }

        private List<LuaFunction> ReadPrototypes()
        {
            List<LuaFunction> functions = new List<LuaFunction>();
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                functions.Add(Decode());
            }
            return functions;
        }

        private List<int> ReadDebugLines()
        {
            List<int> debuglines = new List<int>();
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                debuglines.Add(GetInt());
            }
            return debuglines;
        }

        private List<LuaLocal> ReadDebugLocals()
        {
            List<LuaLocal> locals = new List<LuaLocal>();
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                locals.Add(new LuaLocal(GetString(), GetInt(), GetInt()));
            }
            return locals;
        }

        private List<string> ReadDebugUpvalues()
        {
            List<string> upvalues = new List<string>();
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                upvalues.Add(GetString()); // Upvalue name
            }
            return upvalues;
        }

        // check if input is as expected
        private bool Verify()
        {
            // check magic bytes
            if (GetString(4) != "\x1BLua")
            {
                Console.WriteLine("Error, LuaC File Expected!");
                return false;
            }

            // check version
            if (GetByte() != 0x51)
            {
                Console.WriteLine("Error, Only Lua with version 5.1 is supported!");
                return false;
            }

            GetByte(); // another bytecode
            this.BigEndian = GetByte() == 0;
            this.IntSize = GetByte();
            this.SizeT = GetByte();

            // TODO: figure out what size_t and so are used for

            string bytecodeTarget = GetString(3);
            if (bytecodeTarget != "\x04\x08\x00")
                return false;

            // Index = 11 after this

            return true;
        }

        private void loop()
        {
            while(this.Registers.IP < this.Buffer.Length)
            {
                Console.WriteLine($"{this.Registers.IP.ToString("X4")}: {((LuaOpcode)this.Buffer[this.Registers.IP]).ToString().PadLeft(8)} ...");
                this.InstructionTable[(LuaOpcode)this.Buffer[this.Registers.IP]]();
            }
        }

        // OpCodes

        #region OpcodeHandlers
        private void MOVE()
        {
            this.Stack[this.Registers.A] = this.Stack[this.Registers.B];
            this.Registers.IP++;
        }

        private void LOADK()
        {
            this.Stack[this.Registers.A] = this.Constants[this.Registers.Bx];
            this.Registers.IP++;
        }

        private void LOADBOOL()
        {
            byte val = 1;
            if (this.Registers.B == 0)
                val = 0;

            this.Registers.A = val;

            if (this.Registers.C != 0)
                this.Registers.IP++;
            this.Registers.IP++;
        }

        private void LOADNIL()
        {
            for (int i = this.Registers.A; i < this.Registers.A + this.Registers.B; i++)
                if (this.Stack.ContainsKey(i))
                    this.Stack.Remove(i);
            this.Registers.IP++;
        }

        private void GETUPVAL()
        {
            this.Stack[this.Registers.A] = this.Upvalues[this.Registers.B];
            this.Registers.IP++;
        }

        private void GETGLOBAL()
        {
            //int k = (int)this.Constants[this.Registers.Bx].Data;
            //this.Stack[this.Registers.A] = this.Environment[k];
            //this.Registers.IP++;
        }

        private void GETTABLE()
        {
            //bool C = instruction.C > 0xFF && this.Constants[instruction.C-0xFF]
        }

        private void SETGLOBAL()
        {
            //var k = (int)this.Constants[this.Registers.Bx];
            //this.Environment[k] = this.Stack[this.Registers.A];
            //this.Registers.IP++;
        }

        private void SETUPVAL()
        {
            this.Upvalues[this.Registers.B] = this.Stack[this.Registers.A];
            this.Registers.IP++;
        }

        private void SETTABLE()
        {
            //bool B = instruction.B > 0xFF && this.Constants.ContainsKey(instruction.B - 0x100) || this.Stack[instruction.B];
            //bool C = instruction.C > 0xFF && this.Constants.ContainsKey(instruction.C - 0x100) || this.Stack[instruction.C];
        }

        private void NEWTABLE()
        {
            this.Stack[this.Registers.A] = new Dictionary<int, object>();
        }

        private void SELF()
        {

        }

        private void ADD()
        {
            // test
            this.Registers.A++;
            this.Registers.B++;
            this.Registers.C++;
            this.Registers.IP++;
        }
        private void SUB()
        {

        }
        private void MUL()
        {

        }
        private void DIV()
        {

        }
        private void MOD()
        {

        }
        private void POW()
        {

        }
        private void UNM()
        {
            this.Stack[this.Registers.A] = -Math.Abs((int)this.Stack[this.Registers.B]);
            this.Registers.IP++;
        }
        private void NOT()
        {
            int val = 0;
            if ((int)this.Stack[this.Registers.B] == 0)
                val = 1;

            this.Stack[this.Registers.A] = val;
            this.Registers.IP++;
        }
        private void LEN()
        {
            var table = (Dictionary<int, object>)this.Stack[this.Registers.B];
            this.Stack[this.Registers.A] = table.Count;
            this.Registers.IP++;
        }
        private void CONCAT()
        {
            string result = (string)this.Stack[this.Registers.B];
            for(int i = this.Registers.B; i < this.Registers.C; i++)
                result += (char)this.Stack[i];
            this.Stack[this.Registers.A] = result;
            this.Registers.IP++;
        }
        private void JUMP()
        {
            this.Registers.IP += this.Registers.sBx;
        }
        private void EQ()
        {

        }
        private void LT()
        {

        }
        private void LE()
        {

        }
        private void TEST()
        {
            int A = (int)this.Stack[this.Registers.A];
            if ((A == 1) == (this.Registers.C == 0))
                this.Registers.IP++;
            this.Registers.IP++;
        }
        private void TESTSET()
        {
            int B = (int)this.Stack[this.Registers.B];
            if ((B == 1) == (this.Registers.C == 0))
                this.Registers.IP++;
            else
                this.Stack[this.Registers.A] = B;
            this.Registers.IP++;
        }
        private void CALL()
        {

        }
        private void TAILCALL()
        {

        }
        private void RETURN()
        {

        }
        private void FORLOOP()
        {

        }
        private void FORPREP()
        {

        }
        private void TFORLOOP()
        {

        }
        private void SETLIST()
        {

        }
        private void CLOSE()
        {

        }
        private void CLOSURE()
        {

        }
        private void VARARG()
        {
            //for(int i = instruction.A; i < instruction.A + (instruction.B > 0 && instruction.B-1))
            //{
            //    this.Stack[i] this.;
            //}
        }
        #endregion OpcodeHandlers


        // Helpers

        #region Helpers
        private int GetBits(int input, int n, int n2 = -1)
        {
            if (n2 != -1)
            {
                int total = 0;
                int digitn = 0;
                for(int i = n; i < n2; i++)
                {
                    total += 2 ^ digitn * GetBits(input, i);
                }
                return total;
            }
            else
            {
                int pn = 2 ^ (n - 1);
                bool res = false;
                if(pn != 0)
                    res = ((input % (pn + pn) >= pn));
                //bool res = ((input % (pn + pn) >= pn) && 1 || 0);
                if (res)
                    return 1;
                return 0;
            }

        }

        private byte GetByte()
        {
            this.Index++;
            return this.Buffer[this.Index - 1];
        }

        private int GetInt()
        {
            this.Index += 4;
            return BitConverter.ToInt32(this.Buffer, this.Index - 4);
        }

        private float GetFloat()
        {
            this.Index += 4;
            return BitConverter.ToSingle(this.Buffer, this.Index - 4);
        }

        private long GetLong()
        {
            this.Index += 8;
            return BitConverter.ToInt64(this.Buffer, this.Index - 8);
        }

        private string GetString(long len = 0)
        {
            if(len == 0)
            {
                if(this.SizeT == 4)
                    len = GetInt(); // get_size_t (4 byte?)
                else if (this.SizeT == 8)
                    len = GetLong(); // get_size_t (8 byte?)
            }
                
            string str = "";
            for(int i = 0; i < len; i++)
                str += (char)this.Buffer[this.Index+i];
            this.Index += (int)len;
            return str;
        }
        #endregion
    }
}
