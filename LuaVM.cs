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
            this.Constants = new List<String>();
            this.Prototypes = new List<String>();
            this.DebugLines = new List<String>();
        }
    }

    public class LuaVM
    {
        private byte[] Buffer;
        private int IP;
        private Chunk Chunk;
        private LuaRegisters Registers;
        private Dictionary<int, object> Stack;
        private Dictionary<int, object> Constants;
        private Dictionary<int, object> Upvalues;
        private Dictionary<int, object> Environment;
        private Dictionary<OpcodeName, Action<LuaInstruction>> InstructionTable;
        

        public LuaVM(byte[] LuaC)
        {
            this.Buffer = LuaC;
            this.IP = 0;
            this.Chunk = new Chunk();
            this.Registers = new LuaRegisters();
            this.Stack = new Dictionary<int, object>();
            this.Constants = new Dictionary<int, object>();
            this.Upvalues = new Dictionary<int, object>();
            this.Environment = new Dictionary<int, object>();
            this.InstructionTable = new Dictionary<OpcodeName, Action<LuaInstruction>>()
            {
                {OpcodeName.MOVE, (instr) => {MOVE(instr); } },
                {OpcodeName.LOADK, (instr) => {LOADK(instr); } },
                {OpcodeName.LOADBOOL, (instr) => {LOADBOOL(instr); } },
                {OpcodeName.LOADNIL, (instr) => {LOADNIL(instr); } },
                {OpcodeName.GETUPVAL, (instr) => {GETUPVAL(instr); } },
                {OpcodeName.GETGLOBAL, (instr) => {GETGLOBAL(instr); } },
                {OpcodeName.GETTABLE, (instr) => {GETTABLE(instr); } },
                {OpcodeName.SETGLOBAL, (instr) => {SETGLOBAL(instr); } },
                {OpcodeName.SETUPVAL, (instr) => {SETUPVAL(instr); } },
                {OpcodeName.SETTABLE, (instr) => {SETTABLE(instr); } },
                {OpcodeName.NEWTABLE, (instr) => {NEWTABLE(instr); } },
                {OpcodeName.SELF, (instr) => {SELF(instr); } },
                {OpcodeName.ADD, (instr) => {ADD(instr); } },
                {OpcodeName.SUB, (instr) => {SUB(instr); } },
                {OpcodeName.MUL, (instr) => {MUL(instr); } },
                {OpcodeName.DIV, (instr) => {DIV(instr); } },
                {OpcodeName.MOD, (instr) => {MOD(instr); } },
                {OpcodeName.POW, (instr) => {POW(instr); } },
                {OpcodeName.UNM, (instr) => {UNM(instr); } },
                {OpcodeName.NOT, (instr) => {NOT(instr); } },
                {OpcodeName.LEN, (instr) => {LEN(instr); } },
                {OpcodeName.CONCAT, (instr) => {CONCAT(instr); } },
                {OpcodeName.JMP, (instr) => {JUMP(instr); } },
                {OpcodeName.EQ, (instr) => {EQ(instr); } },
                {OpcodeName.LT, (instr) => {LT(instr); } },
                {OpcodeName.LE, (instr) => {LE(instr); } },
                {OpcodeName.TEST, (instr) => {TEST(instr); } },
                {OpcodeName.TESTSET, (instr) => {TESTSET(instr); } },
                {OpcodeName.CALL, (instr) => {CALL(instr); } },
                {OpcodeName.TAILCALL, (instr) => {TAILCALL(instr); } },
                {OpcodeName.RETURN, (instr) => {RETURN(instr); } },
                {OpcodeName.FORLOOP, (instr) => {FORLOOP(instr); } },
                {OpcodeName.FORPREP, (instr) => {FORPREP(instr); } },
                {OpcodeName.TFORLOOP, (instr) => {TFORLOOP(instr); } },
                {OpcodeName.SETLIST, (instr) => {SETLIST(instr); } },
                {OpcodeName.CLOSE, (instr) => {CLOSE(instr); } },
                {OpcodeName.CLOSURE, (instr) => {CLOSURE(instr); } },
                {OpcodeName.VARARG, (instr) => {VARARG(instr); } },
            };
        }
        
        public void Decode()
        {
            int num = 0;
        }

        // OpCodes

        #region OpcodeHandlers
        private void MOVE(LuaInstruction instruction)
        {
            this.Stack[this.Registers.A] = this.Stack[this.Registers.B];
            this.IP++;
        }

        private void LOADK(LuaInstruction instruction)
        {
            this.Stack[this.Registers.A] = this.Constants[this.Registers.Bx];
            this.IP++;
        }

        private void LOADBOOL(LuaInstruction instruction)
        {
            byte val = 1;
            if (this.Registers.B == 0)
                val = 0;

            this.Registers.A = val;

            if (this.Registers.C != 0)
                this.IP++;
            this.IP++;
        }

        private void LOADNIL(LuaInstruction instruction)
        {
            for (int i = this.Registers.A; i < this.Registers.A + this.Registers.B; i++)
                if (this.Stack.ContainsKey(i))
                    this.Stack.Remove(i);
            this.IP++;
        }

        private void GETUPVAL(LuaInstruction instruction)
        {
            this.Stack[this.Registers.A] = this.Upvalues[this.Registers.B];
            this.IP++;
        }

        private void GETGLOBAL(LuaInstruction instruction)
        {
            int k = (int)this.Constants[instruction.Bx];
            this.Stack[instruction.A] = this.Environment[k];
            this.IP++;
        }

        private void GETTABLE(LuaInstruction instruction)
        {
            //bool C = instruction.C > 0xFF && this.Constants[instruction.C-0xFF]
        }

        private void SETGLOBAL(LuaInstruction instruction)
        {
            var k = (int)this.Constants[instruction.Bx];
            this.Environment[k] = this.Stack[instruction.A];
            this.IP++;
        }

        private void SETUPVAL(LuaInstruction instruction)
        {
            this.Upvalues[instruction.B] = this.Stack[instruction.A];
            this.IP++;
        }

        private void SETTABLE(LuaInstruction instruction)
        {
            //bool B = instruction.B > 0xFF && this.Constants.ContainsKey(instruction.B - 0x100) || this.Stack[instruction.B];
            //bool C = instruction.C > 0xFF && this.Constants.ContainsKey(instruction.C - 0x100) || this.Stack[instruction.C];
        }

        private void NEWTABLE(LuaInstruction instruction)
        {
            this.Stack[instruction.A] = new Dictionary<int, object>();
        }

        private void SELF(LuaInstruction instruction)
        {

        }

        private void ADD(LuaInstruction instruction)
        {

        }
        private void SUB(LuaInstruction instruction)
        {

        }
        private void MUL(LuaInstruction instruction)
        {

        }
        private void DIV(LuaInstruction instruction)
        {

        }
        private void MOD(LuaInstruction instruction)
        {

        }
        private void POW(LuaInstruction instruction)
        {

        }
        private void UNM(LuaInstruction instruction)
        {

        }
        private void NOT(LuaInstruction instruction)
        {

        }
        private void LEN(LuaInstruction instruction)
        {

        }
        private void CONCAT(LuaInstruction instruction)
        {

        }
        private void JUMP(LuaInstruction instruction)
        {

        }
        private void EQ(LuaInstruction instruction)
        {

        }
        private void LT(LuaInstruction instruction)
        {

        }
        private void LE(LuaInstruction instruction)
        {

        }
        private void TEST(LuaInstruction instruction)
        {

        }
        private void TESTSET(LuaInstruction instruction)
        {

        }
        private void CALL(LuaInstruction instruction)
        {

        }
        private void TAILCALL(LuaInstruction instruction)
        {

        }
        private void RETURN(LuaInstruction instruction)
        {

        }
        private void FORLOOP(LuaInstruction instruction)
        {

        }
        private void FORPREP(LuaInstruction instruction)
        {

        }
        private void TFORLOOP(LuaInstruction instruction)
        {

        }
        private void SETLIST(LuaInstruction instruction)
        {

        }
        private void CLOSE(LuaInstruction instruction)
        {

        }
        private void CLOSURE(LuaInstruction instruction)
        {

        }
        private void VARARG(LuaInstruction instruction)
        {

        }
        #endregion OpcodeHandlers


        // Helpers

        #region Helpers
        private byte get_byte()
        {
            this.IP++;
            return this.Buffer[this.IP - 1];
        }

        private int get_int()
        {
            this.IP += 4;
            return BitConverter.ToInt32(this.Buffer, this.IP - 4);
        }

        private float get_float()
        {
            this.IP += 4;
            return BitConverter.ToSingle(this.Buffer, this.IP - 4);
        }

        private long get_long()
        {
            this.IP += 8;
            return BitConverter.ToInt64(this.Buffer, this.IP - 8);
        }

        private string get_string(byte len = 0)
        {
            if(len == 0)
                len = get_byte();
            string str = "";
            for(int i = 0; i < len; i++)
                str += (char)this.Buffer[i];
            this.IP += len;
            return str;
        }
        #endregion

    }
}
