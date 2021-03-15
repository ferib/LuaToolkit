using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;
using LuaToolkit.Models;

namespace LuaToolkit.Decompiler
{
    public class LuaScriptBlock
    {
        public List<int> JumpsFrom;
        public int JumpsTo = -1; // -1 will never happen or its inf loop (iirc)
        public int JumpsNext = -1; // the next instruction (if any)
        public int StartAddress;
        public bool IsChainedIf = false;
        public int IfChainIndex = -1;
        public bool IsChainedIfStart = false;

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

        public LuaFunction Func;
        public LuaDecoder Decoder;

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
            if (l.IsBranch() || l.Instr.OpCode == LuaOpcode.TFORLOOP || l.Instr.OpCode == LuaOpcode.FORLOOP)
            {
                this.JumpsTo = this.StartAddress + this.lines.Count + (short)l.Instr.sBx; // base + offset
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

        // optimize IF or TAILCALL blocks
        public void Optimize()
        {
            // TODO: optimize for condition
            if (this.Lines.Count <= 2)
                return;
            var cLine = this.GetConditionLine();

            string tmpResult = "";
            List<string> result = new List<string>();
            // optimize block in one single line

            // get branch info
            if (!cLine.IsCondition())
                return;

            int varA = -1;
            int varB = -1;

            if (cLine.Instr.OpCode == LuaOpcode.TEST || cLine.Instr.OpCode == LuaOpcode.TESTSET) // only use A?
                varA = cLine.Instr.A;
            else
                if ((cLine.Instr.B & 1 << 8) == 0) // not const
                    varA = cLine.Instr.B;
                if ((cLine.Instr.C & 1 << 8) == 0) // not const
                    varB = cLine.Instr.C;

            // transplants lines
            List<LuaScriptLine> tLines = new List<LuaScriptLine>();
            for (int i = this.Lines.Count - 2; i >= 0; i--)
            {
                // NOTE: Text bugs out, TOOD: fix Text
                if (varA != -1)
                {
                    // check if block contains the variable
                    // TODO: clean this up or make OO??
                    for(int j = 0; j < this.Lines.Count; j++)
                    {
                        if (j == i)
                            continue; // skip this
                        if(this.Lines[j].Op1.Contains("var" + varA) || this.Lines[j].Op2.Contains("var" + varA) || this.Lines[j].Op3.Contains("var" + varA))
                        {
                            this.Lines[i].Op1 = this.Lines[i].Op1.Replace("var" + varA, "var" + this.IfChainIndex + varA);
                            this.Lines[i].Op2 = this.Lines[i].Op2.Replace("var" + varA, "var" + this.IfChainIndex + varA);
                            this.Lines[i].Op3 = this.Lines[i].Op3.Replace("var" + varA, "var" + this.IfChainIndex + varA);
                            break;
                        }
                    }
                } 
                if (varB != -1)
                {
                    // check if block contains the variable
                    // TODO: clean this up or make OO??
                    for (int j = 0; j < this.Lines.Count; j++)
                    {
                        if (j == i)
                            continue; // skip this
                        if (this.Lines[j].Op1.Contains("var" + varB) || this.Lines[j].Op2.Contains("var" + varB) || this.Lines[j].Op3.Contains("var" + varB))
                        {
                            this.Lines[i].Op1 = this.Lines[i].Op1.Replace("var" + varB, "var" + this.IfChainIndex + varB);
                            this.Lines[i].Op2 = this.Lines[i].Op2.Replace("var" + varB, "var" + this.IfChainIndex + varB);
                            this.Lines[i].Op3 = this.Lines[i].Op3.Replace("var" + varB, "var" + this.IfChainIndex + varB);
                            break;
                        }
                    }
                }
            }
            tLines.AddRange(this.Lines.GetRange(0, this.Lines.Count - 2)); // copy transplants
            this.Lines.RemoveRange(0, this.Lines.Count - 2); // remove transplants
            var thisIndex = this.Func.ScriptFunction.Blocks.IndexOf(this);
            this.Func.ScriptFunction.Blocks[thisIndex - this.IfChainIndex].Lines.InsertRange(this.Func.ScriptFunction.Blocks[thisIndex - this.IfChainIndex].Lines.Count - 2, tLines); // complete transplant

            // add copys to restore
            // NOTE: this plan is failure, we need to copy the variables to multiple locations, IF Body and IF End block.
            // the variables can already be overwritten by then!

            //if(this.Func.ScriptFunction.Blocks.Count > thisIndex+1 && this.Func.ScriptFunction.Blocks[thisIndex+1].IfChainIndex == -1)
            //{
            //    if (varA != -1)
            //        this.Lines.Add(new LuaScriptLine($"var{varA} = var{this.IfChainIndex}{varA}"));
            //    //tLines.Add(new LuaScriptLine($"var{varA} = var{this.IfChainIndex}{varA}"));
            //    if (varB != -1)
            //        this.Lines.Add(new LuaScriptLine($"var{varB} = var{this.IfChainIndex}{varB}"));
            //    //tLines.Add(new LuaScriptLine($"var{varB} = var{this.IfChainIndex}{varB}"));
            //}
        }

        public string ToString()
        {
            return $"{this.StartAddress.ToString("0000")}: JMP: {this.JumpsTo}, ELSE: {this.JumpsNext}";
        }
    }
}
