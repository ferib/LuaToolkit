using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Models;

namespace LuaSharpVM.Decompiler
{
    public class LuaScriptBlock
    {
        public List<int> JumpsFrom;
        public int JumpsTo;
        public int StartAddress;

        private int tabIndex;
        public int TabIndex
        {
            get { return this.tabIndex; }
            set
            {
                this.tabIndex = value;
                foreach (var l in this.Lines)
                    l.Depth = this.tabIndex;
            }
        }

        public string Text
        {
            get { return GetText();  }
        }

        private LuaFunction Func;
        private LuaDecoder Decoder;

        public List<LuaScriptLine> Lines;

        public LuaScriptBlock(int address, ref LuaDecoder decoder, ref LuaFunction func)
        {
            this.Decoder = decoder;
            this.Func = func;
            this.StartAddress = address;
            this.Lines = new List<LuaScriptLine>();
        }

        public bool AddScriptLine(LuaScriptLine l)
        {
            this.Lines.Add(l);
            if (l.IsBranch())
            {
                this.JumpsTo = this.StartAddress + this.Lines.Count + l.Instr.sBx; // base + offset
                return true;
            }
            return false;  
        }

        private string GetText()
        {
            string result = "";
            foreach (var l in this.Lines)
                result += l.Text;
            return result;
        }
    }
}
