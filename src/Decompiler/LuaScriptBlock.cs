using System;
using System.Collections.Generic;
using System.Linq;
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
            // this only checks for outgoing, we split incommmings somwhere else
            this.lines.Add(l);
            if (l.IsBranch())
            {
                this.JumpsTo = this.StartAddress + this.lines.Count + l.Instr.sBx; // base + offset
                return true;
            }
            return false;  
        }

        public void RewriteVariables(int offset)
        {
            // Rewrite the variables x by adding mov prefix+x = x and then replacing all x by prefix+x
            List<int> changedVariables = new List<int>();
            for(int i = 0; i < this.Lines.Count; i++)
            {
                LuaInstruction fake = new LuaInstruction(this.Lines[i].Instr.Data);
                changedVariables.AddRange(fake.OffsetVariables(offset));
                this.Lines[i].SetMain(fake);
            }
            var vars = changedVariables.Distinct();
            foreach(var v in vars)
            {
                // TODO: add instr to start!
            }


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

        public LuaScriptLine GetBranchLine() // second last line
        {
            if (this.Lines.Count > 0)
                return this.Lines[this.lines.Count - 1];
            return null;
        }

        public LuaScriptLine GetConditionLine() // last line
        {
            if (this.Lines.Count > 1)
                return this.Lines[this.lines.Count - 2];
            return null;
        }

        public void Optimize()
        {
            // TODO: optimize for condition
        }

        public string ToString()
        {
            return $"{this.StartAddress.ToString("0000")}: JMP: {this.JumpsTo}, ELSE: {this.JumpsNext}";
        }
    }
}
