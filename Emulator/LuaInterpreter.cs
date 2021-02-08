using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Models;

namespace LuaSharpVM.Emulator
{
    public class LuaInterpreter
    {

        private bool BigEndian;
        private int IntSize;
        private int SizeT;
        private int Index;
        private byte[] Buffer;
        private LuaFunction Functions;
        private LuaRegisters Registers;
        private Dictionary<int, object> Stack;
        //private new List<LuaConstant> Constants;
        private Dictionary<int, object> Upvalues;
        private Dictionary<int, object> Environment;
        private Dictionary<LuaOpcode, Action> InstructionTable;

        public LuaInterpreter()
        {
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
            //this.Stack[this.Registers.A] = this.Constants[this.Registers.Bx];
            //this.Registers.IP++;
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
            for (int i = this.Registers.B; i < this.Registers.C; i++)
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

    }
}
