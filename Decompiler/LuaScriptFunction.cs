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

        public List<LuaScriptLine> Lines;

        private string _text;

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
            return $"\n\r" + (this.IsLocal ? "local" : "") + $"function {this.Name}\n\r";
            //return $"\n\rfunction {this.Name}()\n\r";
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
                        //this.Lines[i].Text = ""; // hide JMP's, only used for debugging output
                        this.Lines[i].Op1 = "-- JMP " + (short)(this.Lines[i].Instr.sBx);

                        // TODO: define end of IF
                        bool isEnd = true;
                        switch(this.Lines[i-1].Instr.OpCode)
                        {
                            case LuaOpcode.EQ:
                            case LuaOpcode.LT:
                            case LuaOpcode.LE:
                                isEnd = false;
                            break;
                        }

                        // TODO: identify better
                        if (isEnd)
                            this.Lines[i + 1 + (short)this.Lines[i].Instr.sBx].Op1 = "end\n\r" + this.Lines[i + 1 + (short)this.Lines[i].Instr.sBx].Op1;

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
                        break;
                    case LuaOpcode.RETURN:
                        if(i == this.Lines.Count-1)
                        {
                            this.Lines[i].Text = "end\n\r";
                            this.Lines[i].Depth -= 1;
                        }
                        break;
                }
            }
        }

        public void Beautify()
        {
            // TODO: beautify, optimize, remove dead code?
            Realign();
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

                if(lines[i].StartsWith("if") || lines[i].StartsWith("function"))
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
            Beautify();
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
