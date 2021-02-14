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
        public int JumpsNext = -1; // the next instruction (if any)
        public int StartAddress;

        private int tabIndex;
        public int TabIndex
        {
            get { return this.tabIndex; }
            set
            {
                this.tabIndex = value;
                foreach (var l in this.lines)
                    l.Depth = this.tabIndex;
            }
        }

        public string Text
        {
            get { return GetText();  }
        }

        private LuaFunction Func;
        private LuaDecoder Decoder;

        private List<LuaScriptLine> lines;
        public List<LuaScriptLine> Lines
        {
            get { return this.lines; }
            set { SetLines(value); }
        }

        public LuaScriptBlock(int address, ref LuaDecoder decoder, ref LuaFunction func)
        {
            this.Decoder = decoder;
            this.Func = func;
            this.StartAddress = address;
            this.lines = new List<LuaScriptLine>();
            this.JumpsFrom = new List<int>();
        }

        public bool AddScriptLine(LuaScriptLine l)
        {
            // NOTE: this only checks for outgoing, we need to split incommmings to!!
            this.lines.Add(l);
            if (l.IsBranch())
            {
                this.JumpsTo = this.StartAddress + this.lines.Count + l.Instr.sBx; // base + offset
                return true;
            }
            return false;  
        }

        private void SetLines(List<LuaScriptLine> list)
        {
            this.lines = list;
            if (lines.Count > 1)
                if (lines[lines.Count - 1].IsBranch())
                    this.JumpsTo = this.StartAddress + lines[lines.Count - 1].Instr.sBx; // base + offset
        }

        private string GetText()
        {
            string result = "";
            foreach (var l in this.lines)
                result += l.Text;
            return result;
        }

        public bool HasLineNumber(int index)
        {
            return this.StartAddress < index && index < this.StartAddress + this.lines.Count;
        }

        public LuaScriptLine GetConditionLine() // second last line
        {
            if (this.Lines.Count > 0)
            {
                //if (this.Lines[this.lines.Count - 1].IsBranch()) // last line for JMP, test, testset, etc
                    return this.Lines[this.lines.Count - 1];
                //else if (this.Lines[this.lines.Count - 2].IsBranch()) // IF before JMP
                //    return this.Lines[this.lines.Count - 2];

            }
            return null;
        }

        public LuaScriptLine GetBranchLine() // last line
        {
            if (this.Lines.Count > 1)
            {
                //if (this.Lines[this.lines.Count - 1].IsBranch()) // last line for JMP, test, testset, etc
                return this.Lines[this.lines.Count - 2];
                //else if (this.Lines[this.lines.Count - 2].IsBranch()) // IF before JMP
                //    return this.Lines[this.lines.Count - 2];

            }
            return null;
        }

        public string ToString()
        {
            //if (this.lines.Count < 2 || (this.JumpsTo == -1 && this.JumpsNext == -1))
            //    return $"{this.StartAddress.ToString("0000")}: {this.lines[0]};

            var sLastLine = this.lines[this.lines.Count - 1];


            if (this.JumpsTo == -1 && this.JumpsNext != -1)
                return $"{this.StartAddress.ToString("0000")}: JMP: {this.JumpsNext}";

            //if (this.JumpsTo != -1 && this.JumpsNext != -1)
                return $"{this.StartAddress.ToString("0000")}: JMP: {this.JumpsTo}, ELSE: {this.JumpsNext}";

            // depriciated
            if (sLastLine.Instr.OpCode == LuaOpcode.JMP)
                return $"{this.StartAddress.ToString("0000")}: {this.lines[this.lines.Count - 2]} GOTO {this.JumpsTo}";

            if(sLastLine.Instr.OpCode == LuaOpcode.FORLOOP || sLastLine.Instr.OpCode == LuaOpcode.TFORLOOP)
                return $"{this.StartAddress.ToString("0000")}: (loop) GOTO {this.JumpsTo}";

            

            return $"{this.StartAddress.ToString("0000")}: UNK_GOTO {this.JumpsTo}";
        }
    }
}
