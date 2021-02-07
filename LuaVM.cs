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
        public List<string> Prototypes;
        public List<string> DebugLines;

        public Chunk()
        {
            this.Instructions = new List<LuaInstruction>();
            this.Constants = new List<string>();
            this.Prototypes = new List<string>();
            this.DebugLines = new List<string>();
        }
    }

    public class LuaVM
    {
        private byte[] Buffer;
        private Chunk Chunk;
        private LuaRegisters Registers;
        private Dictionary<int, object> Stack;
        private Dictionary<int, object> Constants;
        private Dictionary<int, object> Upvalues;
        private Dictionary<int, object> Environment;
        private Dictionary<OpcodeName, Action> InstructionTable;
        

        public LuaVM(byte[] LuaC)
        {
            this.Buffer = LuaC;
            this.Chunk = new Chunk();
            this.Registers = new LuaRegisters();
            this.Stack = new Dictionary<int, object>();
            this.Constants = new Dictionary<int, object>();
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
                this.Constants[i] = 0;
                this.Environment[i] = 0;
            }
            loop();
        }

        private void Decode()
        {
            int num = 0;
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
            int k = (int)this.Constants[this.Registers.Bx];
            this.Stack[this.Registers.A] = this.Environment[k];
            this.Registers.IP++;
        }

        private void GETTABLE()
        {
            //bool C = instruction.C > 0xFF && this.Constants[instruction.C-0xFF]
        }

        private void SETGLOBAL()
        {
            var k = (int)this.Constants[this.Registers.Bx];
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
        private byte get_byte()
        {
            this.Registers.IP++;
            return this.Buffer[this.Registers.IP - 1];
        }

        private int get_int()
        {
            this.Registers.IP += 4;
            return BitConverter.ToInt32(this.Buffer, this.Registers.IP - 4);
        }

        private float get_float()
        {
            this.Registers.IP += 4;
            return BitConverter.ToSingle(this.Buffer, this.Registers.IP - 4);
        }

        private long get_long()
        {
            this.Registers.IP += 8;
            return BitConverter.ToInt64(this.Buffer, this.Registers.IP - 8);
        }

        private string get_string(byte len = 0)
        {
            if(len == 0)
                len = get_byte();
            string str = "";
            for(int i = 0; i < len; i++)
                str += (char)this.Buffer[i];
            this.Registers.IP += len;
            return str;
        }
        #endregion

    }
}
