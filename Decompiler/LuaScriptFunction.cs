using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Models;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Decompiler
{
    public class LuaScriptFunction
    {
        public int Depth;
        private LuaDecoder Decoder;
        private LuaFunction Func;
        private string Name;

        public List<LuaScriptLine> Lines;

        public string Text
        {
            get { return GetText(); }
        }

        public LuaScriptFunction(string name, ref LuaFunction func, ref LuaDecoder decoder)
        {
            this.Name = name;
            this.Func = func;
            this.Decoder = decoder;
            this.Lines = new List<LuaScriptLine>();
        }

        public override string ToString()
        {
            return $"\n\rfunction {this.Name}()\n\r";
        }

        public void Beautify()
        {
            // TODO: beautify, optimize, remove dead code?
        }

        public void Reformat()
        {
            // fix if statements
            int LastIf = -1;
            int LastElse = -1; // or whatever we need
            int LastReturn = -1;
            List<int> BlacklistJumps = new List<int>(); // put in jumps from if chains
            for (int i = 0; i < this.Lines.Count; i++)
            {
                switch(this.Lines[i].Instr.OpCode)
                {
                    case LuaOpcode.JMP:

                        this.Lines[i].Text = "";
                        //this.Lines[i].Op1 = "-- JMP " + (short)(this.Lines[i].Instr.sBx);
                        //// place elseif/else/end for if
                        ////if ((short)(this.Lines[i].Instr.sBx) == 0)
                        ////    continue;

                        //this.Lines[i + (short)(this.Lines[i].Instr.sBx) + 1].Op1 = "END; " + this.Lines[i + (short)(this.Lines[i].Instr.sBx) + 1].Op1;

                        //if (this.Lines[i + (short)(this.Lines[i].Instr.sBx)+1].Instr.OpCode == LuaOpcode.JMP)
                        //{
                        //    this.Lines[i + (short)(this.Lines[i].Instr.sBx + 1)].Op1 = "END2; " + this.Lines[i + (short)(this.Lines[i].Instr.sBx) + 1].Op1;
                        //    BlacklistJumps.Add(i + (short)(this.Lines[i].Instr.sBx)+1);
                        //}
                    break;
                    case LuaOpcode.EQ:
                    case LuaOpcode.LT:
                    case LuaOpcode.LE:
                        // NOTE: EQ, LT, LE also increase the PC when true to avoid the first JMP
                        //       which leads to the second part of the if statement
                        if (this.Lines[i + 1].Instr.OpCode == LuaOpcode.JMP)
                        {
                            // else or end
                            string keyword = "end\n\r ";
                            if (this.Lines.Count > (i + 2 + (short)(this.Lines[i + 1].Instr.sBx)))
                            {
                                switch (this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Instr.OpCode)
                                {
                                    case LuaOpcode.EQ:
                                    case LuaOpcode.LT:
                                    case LuaOpcode.LE:
                                        keyword = "else";
                                        break;
                                }
                            }
                            this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Op1 = keyword + "" + this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Op1;
                        }
                        break;
                }
            }
        }

        public void Complete()
        {
            // fix if statements
            Reformat();
            // TODO: Beautify();
        }

        public string GetText()
        {
            string result = this.ToString();
            for (int i = 0; i < this.Lines.Count; i++)
                result += this.Lines[i].Text;
            return result;
        }
    }

}
