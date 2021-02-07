using System;
using System.Collections.Generic;

namespace LuaSharpVM
{
    public class Chunk
    {
        public string Name;
        public int FirstLine;
        public int LastLine;
        public byte Upvalues;
        public byte Arguments;
        public byte Vargs;
        public byte Stack;
        public List<LuaInstruction> Instructions;
        public List<string> Constants;
        public List<Chunk> Prototypes;
        public List<int> DebugLines;

        public Chunk()
        {
            this.Instructions = new List<LuaInstruction>();
            this.Constants = new List<string>();
            this.Prototypes = new List<Chunk>();
            this.DebugLines = new List<int>();
        }
    }

    public class LuaVM
    {
        private byte[] Buffer;
        private Chunk Chunk;
        private LuaRegisters Registers;
        private Dictionary<int, object> Stack;
        private new List<LuaConstant> Constants;
        private Dictionary<int, object> Upvalues;
        private Dictionary<int, object> Environment;
        private Dictionary<OpcodeName, Action> InstructionTable;


        public LuaVM(byte[] LuaC)
        {
            this.Buffer = LuaC;
            this.Chunk = new Chunk();
            this.Registers = new LuaRegisters();
            this.Stack = new Dictionary<int, object>();
            this.Constants = new List<LuaConstant>();
            this.Upvalues = new Dictionary<int, object>();
            this.Environment = new Dictionary<int, object>();
            this.InstructionTable = new Dictionary<OpcodeName, Action>()
            {
                {OpcodeName.MOVE, () => {MOVE(); } },
                {OpcodeName.LOADK, () => {LOADK(); } },
                {OpcodeName.LOADBOOL, () => {LOADBOOL(); } },
                {OpcodeName.LOADNIL, () => {LOADNIL(); } },
                {OpcodeName.GETUPVAL, () => {GETUPVAL(); } },
                {OpcodeName.GETGLOBAL, () => {GETGLOBAL(); } },
                {OpcodeName.GETTABLE, () => {GETTABLE(); } },
                {OpcodeName.SETGLOBAL, () => {SETGLOBAL(); } },
                {OpcodeName.SETUPVAL, () => {SETUPVAL(); } },
                {OpcodeName.SETTABLE, () => {SETTABLE(); } },
                {OpcodeName.NEWTABLE, () => {NEWTABLE(); } },
                {OpcodeName.SELF, () => {SELF(); } },
                {OpcodeName.ADD, () => {ADD(); } },
                {OpcodeName.SUB, () => {SUB(); } },
                {OpcodeName.MUL, () => {MUL(); } },
                {OpcodeName.DIV, () => {DIV(); } },
                {OpcodeName.MOD, () => {MOD(); } },
                {OpcodeName.POW, () => {POW(); } },
                {OpcodeName.UNM, () => {UNM(); } },
                {OpcodeName.NOT, () => {NOT(); } },
                {OpcodeName.LEN, () => {LEN(); } },
                {OpcodeName.CONCAT, () => {CONCAT(); } },
                {OpcodeName.JMP, () => {JUMP(); } },
                {OpcodeName.EQ, () => {EQ(); } },
                {OpcodeName.LT, () => {LT(); } },
                {OpcodeName.LE, () => {LE(); } },
                {OpcodeName.TEST, () => {TEST(); } },
                {OpcodeName.TESTSET, () => {TESTSET(); } },
                {OpcodeName.CALL, () => {CALL(); } },
                {OpcodeName.TAILCALL, () => {TAILCALL(); } },
                {OpcodeName.RETURN, () => {RETURN(); } },
                {OpcodeName.FORLOOP, () => {FORLOOP(); } },
                {OpcodeName.FORPREP, () => {FORPREP(); } },
                {OpcodeName.TFORLOOP, () => {TFORLOOP(); } },
                {OpcodeName.SETLIST, () => {SETLIST(); } },
                {OpcodeName.CLOSE, () => {CLOSE(); } },
                {OpcodeName.CLOSURE, () => {CLOSURE(); } },
                {OpcodeName.VARARG, () => {VARARG(); } },
            };
        }

        public void Execute()
        {
            // fill with 0's
            for(int i = 0; i < 0xFF; i++)
            {
                this.Stack[i] = 0;
                this.Environment[i] = 0;
            }
            //Decode(); // init the Lua stuff
            loop(); // execute the bytecode
        }

        // decode the metadata and what not from the LuaC file
        private Chunk Decode()
        {
            Chunk Chunk = new Chunk();

            int count = 0;
            Chunk.Name = GetString();     // Function name
            Chunk.FirstLine = GetInt();   // First line
            Chunk.LastLine = GetInt();    // Last line

            // TODO: skip first 2 bytes of this.Chunk.Name
            if(Chunk.Name != "")
                Chunk.Name = Chunk.Name.Substring(2);

            // point around
            Chunk.Upvalues = GetByte();
            Chunk.Arguments = GetByte();
            Chunk.Vargs = GetByte();
            Chunk.Stack = GetByte();

            // Decode Instructions
            count = GetInt();
            for (int i = 0; i < count; i++)
            {
                LuaInstruction instr = new LuaInstruction();
                int data = GetInt();
                instr.Opcode = (OpcodeName)GetBits(data, 1, 6);
                instr.Type = LuaInstructions.Table[(int)instr.Opcode].Type;
                instr.A = GetBits(data, 7, 14);

                // convert
                switch(instr.Type)
                {
                    case OpcodeType.ABC:
                        instr.B = GetBits(data, 24, 32);
                        instr.C = GetBits(data, 15, 23);
                        break;
                    case OpcodeType.ABx:
                        instr.Bx = GetBits(data, 15, 32);
                        break;
                    case OpcodeType.AsBx:
                        instr.sBx = GetBits(data, 15, 32) - 0x1FFFF;
                        break;
                }
                Chunk.Instructions.Add(instr);
            }

            // Decode constants
            count = GetInt();
            for(int i = 0; i < count; i++)
            {
                var constant = new LuaConstant();
                constant.Type = (ConstantType)GetByte();

                switch(constant.Type)
                {
                    case ConstantType.BOOL: // bool
                        constant.Data = GetByte() != 0;
                        break;
                    case ConstantType.FLOAT: // float
                        constant.Data = GetFloat();
                        break;
                    case ConstantType.STRING: // string
                        constant.Data = GetString().Substring(2);
                        break;
                }

                this.Constants.Add(constant);
            }

            // Decode prototypes
            count = GetInt();
            for (int i = 0; i < count; i++)
            {
                Chunk.Prototypes.Add(Decode());
            }

            // Decode debuginfo: Line Numbers
            count = GetInt();
            for(int i = 0; i < count; i++)
            {
                Chunk.DebugLines.Add(GetInt());
            }

            // Decode debuginfo: Locals
            count = GetInt();
            for (int i = 0; i < count; i++)
            {
                GetString(); // Local name
                GetInt();   // Start PC
                GetInt();   // End PC
                // TODO: store this info?
            }

            // Decode debuginfo: Upvalues
            count = GetInt();
            for (int i = 0; i < count; i++)
            {
                GetString(); // Upvalue name
                // TODO: Store this
            }

            return Chunk;
        }

        private void loop()
        {
            while(this.Registers.IP < this.Buffer.Length)
            {
                Console.WriteLine($"{this.Registers.IP.ToString("X4")}: {((OpcodeName)this.Buffer[this.Registers.IP]).ToString().PadLeft(8)} ...");
                this.InstructionTable[(OpcodeName)this.Buffer[this.Registers.IP]]();
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
            int k = (int)this.Constants[this.Registers.Bx].Data;
            this.Stack[this.Registers.A] = this.Environment[k];
            this.Registers.IP++;
        }

        private void GETTABLE()
        {
            //bool C = instruction.C > 0xFF && this.Constants[instruction.C-0xFF]
        }

        private void SETGLOBAL()
        {
            var k = (int)this.Constants[this.Registers.Bx].Data;
            this.Environment[k] = this.Stack[this.Registers.A];
            this.Registers.IP++;
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
                bool res = ((input % (pn + pn) >= pn));
                //bool res = ((input % (pn + pn) >= pn) && 1 || 0);
                if (res)
                    return 1;
                return 0;
            }

        }
        private byte GetByte()
        {
            this.Registers.IP++;
            return this.Buffer[this.Registers.IP - 1];
        }

        private int GetInt()
        {
            this.Registers.IP += 4;
            return BitConverter.ToInt32(this.Buffer, this.Registers.IP - 4);
        }

        private float GetFloat()
        {
            this.Registers.IP += 4;
            return BitConverter.ToSingle(this.Buffer, this.Registers.IP - 4);
        }

        private long GetLong()
        {
            this.Registers.IP += 8;
            return BitConverter.ToInt64(this.Buffer, this.Registers.IP - 8);
        }

        private string GetString(byte len = 0)
        {
            if(len == 0)
                len = GetByte();
            string str = "";
            for(int i = 0; i < len; i++)
                str += (char)this.Buffer[i];
            this.Registers.IP += len;
            return str;
        }
        #endregion

    }
}
