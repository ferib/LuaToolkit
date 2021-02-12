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
        private bool IsLocal = false;
        private List<string> Args;
        public List<LuaScriptLine> Lines;

        private string _text;

        public string Text
        {
            get { return GetText(); }
        }

        public LuaScriptFunction(string name, List<string> args, ref LuaFunction func, ref LuaDecoder decoder)
        {
            this.Name = name;
            this.Args = args;
            this.Func = func;
            this.Decoder = decoder;
            this.Lines = new List<LuaScriptLine>();
        }

        public override string ToString()
        {
            if (this.Name == null && GetName() != null)
                return "-- root file\n\r";

            string args = "(";
            for(int i = 0; i < this.Args.Count; i++)
            {
                args += this.Args[i];
                if (i < this.Args.Count - 1)
                    args += ", "; 
            }
            args += ")";
            return $"\n\r" + (this.IsLocal ? "local" : "") + $"function {GetName()}{args}\n\r";
        }

        private string GetName()
        {
            if (Func.Name != null && Func.Name != "")
                return Func.Name;
            return this.Name;
        }

        public void Reformat()
        {
            // fix if statements
            for (int i = 0; i < this.Lines.Count; i++)
            {
                switch(this.Lines[i].Instr.OpCode)
                {
                    case LuaOpcode.FORLOOP:
                        // NOTE: this get beutifyd anyways
                        break;
                    case LuaOpcode.JMP:
                        //this.Lines[i].Text = ""; // hide JMP's, only used for debugging output
                        //this.Lines[i].Op1 = "-- JMP " + (short)(this.Lines[i].Instr.sBx);

                        // TODO: replace Op1 & Op3 for multilign if's
                        bool isEnd = true;
                        bool isElse = false;
                        switch (this.Lines[i - 1].Instr.OpCode)
                        {
                            case LuaOpcode.EQ: // end detection
                            case LuaOpcode.LT:
                            case LuaOpcode.LE:
                                isEnd = false;
                                break;
                        }
                        if(i > (short)this.Lines[i].Instr.sBx+1) // else detected when JMP leads to 'IF;JMP'
                            if (this.Lines[i - (short)this.Lines[i].Instr.sBx - 1].Instr.OpCode == LuaOpcode.JMP)
                                switch (this.Lines[i - (short)this.Lines[i].Instr.sBx - 2].Instr.OpCode)
                                {
                                    case LuaOpcode.EQ: // else detection
                                    case LuaOpcode.LT:
                                    case LuaOpcode.LE:
                                        isElse = true;
                                        this.Lines[i].Op1 = "else";
                                        break;
                                }

                        // TODO: fix 
                        if (this.Lines[i - 1].Instr.OpCode == LuaOpcode.TFORLOOP)
                        {
                            var loopStart = this.Lines[i - 1 + this.Lines[i].Instr.sBx];
                            loopStart.Op3 += $"\n\r{new string('\t', loopStart.Depth)}for {this.Lines[i - 1].Op2} in {this.Lines[i - 1].Op3} do";
                            this.Lines[i - 1].Op1 = "end";
                            this.Lines[i - 1].Op2 = ""; // erase
                            this.Lines[i - 1].Op3 = "";
                        }
                        if (isEnd)
                            this.Lines[i + 1 + (short)this.Lines[i].Instr.sBx].Op1 = "end\n\r" + this.Lines[i + 1 + (short)this.Lines[i].Instr.sBx].Op1;
                        break;
                    case LuaOpcode.EQ:
                    case LuaOpcode.LT:
                    case LuaOpcode.LE:
                        // NOTE: EQ, LT, LE also increase the PC when true to avoid the first JMP
                        //       which leads to the second part of the if statement
                        if (this.Lines[i + 1].Instr.OpCode == LuaOpcode.JMP)
                        {
                            // else or end
                            string keyword = $"end\n\r{new string('\t', this.Lines[i].Depth)}";
                            if (this.Lines.Count > (i + 2 + (short)(this.Lines[i + 1].Instr.sBx)))
                            {
                                switch (this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Instr.OpCode)
                                {
                                    case LuaOpcode.EQ:
                                    case LuaOpcode.LT:
                                    case LuaOpcode.LE: // another if for elseif
                                        keyword = "else";
                                        break;
                                }
                            }
                            if (this.Lines[i + 1 + (short)(this.Lines[i + 1].Instr.sBx)].Instr.OpCode == LuaOpcode.JMP)
                            {
                                keyword = $"else\n\r{new string('\t', this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Depth)}"; // JMP indicates there is another block
                            }
                            this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Op1 = keyword + "" + this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Op1;
                        }
                        // pre
                        switch (this.Lines[i -2].Instr.OpCode)
                        {
                            case LuaOpcode.EQ:
                            case LuaOpcode.LT:
                            case LuaOpcode.LE:
                                this.Lines[i].Op1 = ""; // remove 'if'
                                break;
                        }
                        // post
                        switch (this.Lines[i + 2].Instr.OpCode)
                        {
                            case LuaOpcode.EQ:
                            case LuaOpcode.LT:
                            case LuaOpcode.LE:
                                if((short)this.Lines[i + 2].Instr.sBx == 2) // skips 2 in case of OR
                                    this.Lines[i].Op3 = "or"; // define or/and
                                else
                                    this.Lines[i].Op3 = "and";
                                break;
                        }
                        break;
                    case LuaOpcode.RETURN:
                        if (i == this.Lines.Count - 1)
                        {
                            this.Lines[i].Text = "end\n\r";
                            this.Lines[i].Depth -= 1;
                        }
                        break;
                    case LuaOpcode.GETGLOBAL:
                        // replace _G['name'] with func ref name
                        for (int j = i; j < this.Lines.Count; j++)
                        {
                            // try find a mov that references the call
                            if (this.Lines[j].Instr.OpCode != LuaOpcode.CALL)
                                continue;

                            if (this.Lines[j].Instr.A == this.Lines[i].Instr.A) // j.A = i.A; funcname = j.Bx
                            {
                                this.Lines[j].Op2 = this.Lines[i].Op2.Replace("\"", "").Replace("\'", ""); // strip string enocding
                                this.Lines[i].Text = ""; // erase
                            }
                        }
                        break;
                    //case LuaOpcode.CALL:
                    //    // Original: var1(var0); var2 = var1
                    //    // Fixed:    var2 = var1(var0)
                    //    if (i > this.Lines.Count - 1 ||     // is MOVE && mov refs to func return
                    //        this.Lines[i + 1].Instr.OpCode != LuaOpcode.MOVE || this.Lines[i].Instr.A != this.Lines[i + 1].Instr.B)
                    //        break;

                    //    string caller = this.Lines[i].Op1;
                    //    caller = $"{this.Lines[i + 1].Op1} = {caller}";
                    //    this.Lines[i].Op1 = caller;
                    //    this.Lines[i + 1].Text = ""; // erase
                    //    break;
                    case LuaOpcode.LOADNIL:
                        // TODO: remove '= nil', add local and comas: local, v1, v2 v3, v4, v5 
                        break;
                }
            }
        }

        public void Realign()
        {
            // text based because we did wanky things instead of respecting the list
            _text = GetText();
            int tabCount = 1;
            string[] lines = Text.Replace("\r","").Replace("\t","").Split('\n');
            string newText = "";
            for(int i = 0; i < lines.Length; i++)
            {
                //string[] moreLines = lines[i].Split(';');
                bool add = false;
                bool sub = false;

                if(lines[i].StartsWith("if") || lines[i].StartsWith("function") || lines[i].StartsWith("for"))
                {
                    add = true;
                }
                else if(lines[i].StartsWith("else"))
                {
                    if(i < lines.Length-1 && lines[i+1].StartsWith("if"))
                    {
                        // elseif
                        newText += $"{new string('\t', tabCount-1)}{lines[i]}{lines[i+1]}\r\n";
                        i +=1; // brrrr fuck y'all, i skip next one this way!
                        continue;
                    }
                    else
                    {
                        // else
                        tabCount -= 1;
                        add = true;
                    }
                }else if(lines[i].StartsWith("end"))
                {
                    tabCount -= 1;
                }
                if (tabCount < 0)
                    tabCount = 0;

                newText += $"{new string('\t',tabCount)}{lines[i]}\r\n";
                if (add)
                    tabCount += 1;
                if (sub)
                    tabCount -= 1;

            }
            _text = newText;
        }

        public void Complete()
        {
            // fix if statements
            Reformat();
            Realign();
            // TODO: move to Beautify();
        }

        public string GetText()
        {
            if (_text != null)
                return _text; // stores end results

            string result = this.ToString();
            for (int i = 0; i < this.Lines.Count; i++)
                result += this.Lines[i].Text;
            return result;
        }
    }

}
