using System;
using System.Collections.Generic;

namespace LuaSharpVM
{
    public struct LuaInstruction
    {
        public Opcode Opcode;
        public int A;
        public int B;
        public int Bx;
        public int C;
        public int sBx;
    }

    public class LuaVM
    {
        private byte[] Buffer;
        private int Index;
        private Dictionary<Opcode, Action<LuaInstruction>> InstructionTable;

        public LuaVM(byte[] LuaC)
        {
            this.Buffer = LuaC;
            this.Index = 0;
            this.InstructionTable = new Dictionary<Opcode, Action<LuaInstruction>>()
            {
                {Opcode.MOVE, (instr) => {MOVE(instr); } },
                {Opcode.LOADK, (instr) => {LOADK(instr); } },
                {Opcode.LOADBOOL, (instr) => {LOADBOOL(instr); } },
                {Opcode.LOADNIL, (instr) => {LOADNIL(instr); } },
                {Opcode.GETUPVAL, (instr) => {GETUPVAL(instr); } },
                {Opcode.GETGLOBAL, (instr) => {GETGLOBAL(instr); } },
                {Opcode.GETTABLE, (instr) => {GETTABLE(instr); } },
                {Opcode.SETGLOBAL, (instr) => {SETGLOBAL(instr); } },
                {Opcode.SETUPVAL, (instr) => {SETUPVAL(instr); } },
                {Opcode.SETTABLE, (instr) => {SETTABLE(instr); } },
                {Opcode.NEWTABLE, (instr) => {NEWTABLE(instr); } },
                {Opcode.SELF, (instr) => {SELF(instr); } },
                {Opcode.ADD, (instr) => {ADD(instr); } },
                {Opcode.SUB, (instr) => {SUB(instr); } },
                {Opcode.DIV, (instr) => {DIV(instr); } },
                {Opcode.MOD, (instr) => {MOD(instr); } },
                {Opcode.POW, (instr) => {POW(instr); } },
                {Opcode.UNM, (instr) => {UNM(instr); } },
                {Opcode.NOT, (instr) => {NOT(instr); } },
                {Opcode.LEN, (instr) => {LEN(instr); } },
                {Opcode.CONCAT, (instr) => {CONCAT(instr); } },
                {Opcode.JMP, (instr) => {JUMP(instr); } },
                {Opcode.EQ, (instr) => {EQ(instr); } },
                {Opcode.LT, (instr) => {LT(instr); } },
                {Opcode.LE, (instr) => {LE(instr); } },
                {Opcode.TEST, (instr) => {TEST(instr); } },
                {Opcode.TESTSET, (instr) => {TESTSET(instr); } },
                {Opcode.CALL, (instr) => {CALL(instr); } },
                {Opcode.TAILCALL, (instr) => {TAILCALL(instr); } },
                {Opcode.RETURN, (instr) => {RETURN(instr); } },
                {Opcode.FORLOOP, (instr) => {FORLOOP(instr); } },
                {Opcode.TFORLOOP, (instr) => {TFORLOOP(instr); } },
                {Opcode.SETLIST, (instr) => {SETLIST(instr); } },
                {Opcode.CLOSE, (instr) => {CLOSE(instr); } },
                {Opcode.CLOSURE, (instr) => {CLOSURE(instr); } },
                {Opcode.VARARG, (instr) => {VARARG(instr); } },
            };
        }
        
        public void Tick()
        {

        }

        // OpCodes
        private void MOVE(LuaInstruction instruction)
        {

        }

        private void LOADK(LuaInstruction instruction)
        {

        }

        private void LOADBOOL(LuaInstruction instruction)
        {

        }

        private void LOADNIL(LuaInstruction instruction)
        {

        }

        private void GETUPVAL(LuaInstruction instruction)
        {

        }

        private void GETGLOBAL(LuaInstruction instruction)
        {

        }

        private void GETTABLE(LuaInstruction instruction)
        {

        }

        private void SETGLOBAL(LuaInstruction instruction)
        {

        }

        private void SETUPVAL(LuaInstruction instruction)
        {

        }

        private void SETTABLE(LuaInstruction instruction)
        {

        }

        private void NEWTABLE(LuaInstruction instruction)
        {

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

        // Helpers
        private byte get_byte()
        {
            this.Index++;
            return this.Buffer[this.Index - 1];
        }

        private int get_int()
        {
            this.Index += 4;
            return BitConverter.ToInt32(this.Buffer, this.Index - 4);
        }

        private float get_float()
        {
            this.Index += 4;
            return BitConverter.ToSingle(this.Buffer, this.Index - 4);
        }

        private long get_long()
        {
            this.Index += 8;
            return BitConverter.ToInt64(this.Buffer, this.Index - 8);
        }

        private string get_string(byte len = 0)
        {
            if(len == 0)
                len = get_byte();
            string str = "";
            for(int i = 0; i < len; i++)
                str += (char)this.Buffer[i];
            this.Index += len;
            return str;
        }

    }
}
