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
            for (int i = 0; i < this.Lines.Count; i++)
            {
                if (this.Lines[i].Instr.OpCode == LuaOpcode.JMP)
                {
                    // place elseif/else/end for if
                    if ((ushort)this.Lines[i].Instr.sBx != 0 &&
                        this.Lines[i + (ushort)this.Lines[i].Instr.sBx + 1] != null)
                    {
                        this.Lines[i + (ushort)this.Lines[i].Instr.sBx + 1].Text = "END\r\n";
                    }
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
