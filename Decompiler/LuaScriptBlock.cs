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
        public int JumpsTo = -1; // -1 will never happen or its inf loop (iirc)
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
            this.JumpsFrom = new List<int>();
        }

        public bool AddScriptLine(LuaScriptLine l)
        {
            // NOTE: this only checks for outgoing, we need to split incommmings to!!
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

        public bool HasLineNumber(int index)
        {
            return this.StartAddress < index && index < this.StartAddress + this.Lines.Count;
        }

        public string ToString()
        {
            if(this.Lines.Count < 2 || this.JumpsTo == -1)
                return $"{this.StartAddress.ToString("0000")}: The End";

            var sLastLine = this.Lines[this.Lines.Count - 1];

            if(sLastLine.Instr.OpCode == LuaOpcode.JMP)
                return $"{this.StartAddress.ToString("0000")}: {this.Lines[this.Lines.Count - 2]} GOTO {this.JumpsTo}";

            if(sLastLine.Instr.OpCode == LuaOpcode.FORLOOP || sLastLine.Instr.OpCode == LuaOpcode.TFORLOOP)
                return $"{this.StartAddress.ToString("0000")}: (loop) GOTO {this.JumpsTo}";

            return $"{this.StartAddress.ToString("0000")}: UNK_GOTO {this.JumpsTo}";
        }
    }
}
