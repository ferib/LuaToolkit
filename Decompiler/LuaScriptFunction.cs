using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Models;
using LuaSharpVM.Disassembler;
using System.Linq;

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

        public List<LuaScriptBlock> Blocks;

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
            this.Blocks = new List<LuaScriptBlock>();
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
        

        private void GenerateBlocks()
        {
            int index = 0;
            this.Blocks.Clear();
            while(index < this.Lines.Count)
            { 
                LuaScriptBlock b = new LuaScriptBlock(index, ref this.Decoder, ref this.Func);
                while(index < this.Lines.Count)
                {
                    if (b.AddScriptLine(this.Lines[index]))
                        break;
                    index++;
                }
                index++;
                this.Blocks.Add(b); // save block
            }

            // add block jumpsFrom and split
            List<KeyValuePair<int, int>> BlockSplitLines = new List<KeyValuePair<int, int>>();// block, line
            for (int i = 0; i < this.Blocks.Count; i++)
            {
                if (this.Blocks[i].JumpsTo == -1)
                    continue;
                var targets = this.Blocks.FindAll(x => x.HasLineNumber(this.Blocks[i].JumpsTo));
                if(targets.Count > 0)
                    BlockSplitLines.Add(new KeyValuePair<int, int>(this.Blocks.FindIndex(x => x.StartAddress == targets[0].StartAddress), this.Blocks[i].JumpsTo));
                //foreach (var tb in targets)
                //    BlockSplitLines.Add(new KeyValuePair<int, int>(this.Blocks.FindIndex(x => x.StartAddress == tb.StartAddress), i));
            }

            // cut blocks and make new ones
            for (int i = 0; i < BlockSplitLines.Count; i++)
            {
                if (this.Blocks[BlockSplitLines[i].Key].StartAddress + this.Blocks[BlockSplitLines[i].Key].Lines.Count < BlockSplitLines[i].Value &&
                    this.Blocks[BlockSplitLines[i].Key].Lines.Count >= BlockSplitLines[i].Value)
                    continue; // already circumcised this boi

                if (this.Blocks.Find(x => x.StartAddress == BlockSplitLines[i].Value) != null)
                    continue; // already circumcised by another block

                LuaScriptBlock splitBlock = new LuaScriptBlock(BlockSplitLines[i].Value, ref this.Decoder, ref this.Func);
                for(int j = BlockSplitLines[i].Value - this.Blocks[BlockSplitLines[i].Key].StartAddress; j < this.Blocks[BlockSplitLines[i].Key].Lines.Count; j++)
                    splitBlock.Lines.Add(this.Blocks[BlockSplitLines[i].Key].Lines[j]); // copy from old to new
                
                // delete old lines
                if(splitBlock.Lines.Count > 0)
                    this.Blocks[BlockSplitLines[i].Key].Lines.RemoveRange(BlockSplitLines[i].Value - this.Blocks[BlockSplitLines[i].Key].StartAddress, splitBlock.Lines.Count);

                this.Blocks.Insert(BlockSplitLines[i].Key+1, splitBlock); // insert new block after modified one
            }
            // fix JumpsTo and JumpsNext ?
            this.Blocks.OrderBy(x => x.StartAddress);
            for (int i = 0; i < this.Blocks.Count; i++)
            {
                if (i == this.Blocks.Count - 1)
                {
                    // Last block shouldnt jump to anywhere
                    this.Blocks[i].JumpsTo = -1;
                    this.Blocks[i].JumpsNext = -1;
                    continue;
                }

                // pre jmp instruction
                switch(this.Blocks[i].Lines[this.Blocks[i].Lines.Count-2].Instr.OpCode)
                {
                    // TODO: check which instructions dont pick the next one

                    //case LuaOpcode.TFORLOOP:
                    //    this.Blocks[i].JumpsNext = this.Blocks[i].StartAddress + this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 2].Instr.sBx + 1; // TODO: verify math
                    //    this.Blocks[i].JumpsTo = -1; // erase?
                    //    break; // jmp?
                    //case LuaOpcode.LOADBOOL: // pc++
                    //    this.Blocks[i].JumpsNext = 0;
                    //    break;
                    default:
                        // TODO: figure out what other instructions do NOT PC+=1
                        this.Blocks[i].JumpsNext = this.Blocks[i + 1].StartAddress;
                        break;
                }
            }
        }

        public void Complete()
        {
            GenerateBlocks();
            // fix if statements
            //Reformat(); // OLD
            //FixCodeBlocks();
            //Realign(); // correct tabs
        }

        private void Reformat()
        {
            // fix if statements
            for (int i = 0; i < this.Lines.Count; i++)
            {
                switch (this.Lines[i].Instr.OpCode)
                {
                    case LuaOpcode.FORLOOP:
                        // NOTE: this get beutifyd anyways
                        break;
                    case LuaOpcode.JMP:
                        // NOTE: debugging
                        //this.Lines[i].Op1 = "-- JMP " + (short)(this.Lines[i].Instr.sBx);

                        this.Lines[i + 1 + this.Lines[i].Instr.sBx].Op1 = "." + this.Lines[i + 1 + this.Lines[i].Instr.sBx].Op1;
                        this.Lines[i + 1 + this.Lines[i].Instr.sBx].BranchInc.Add(i); // let em know incomming jumps
                        // NOTE: the jump location is indicates the start of a block
                        //       we must collection all starts to identify if body's 
                        //       and to merge if's into eachother

                        /*
                        // IF's ELSE detection
                        if(i > (short)this.Lines[i].Instr.sBx+1) // else detected when JMP leads to 'IF;JMP'
                            if (this.Lines[i - (short)this.Lines[i].Instr.sBx - 1].Instr.OpCode == LuaOpcode.JMP)
                                switch (this.Lines[i - (short)this.Lines[i].Instr.sBx - 2].Instr.OpCode)
                                {
                                    case LuaOpcode.EQ: // else detection
                                    case LuaOpcode.LT:
                                    case LuaOpcode.LE:
                                        this.Lines[i].Op1 = "else";
                                        break;
                                }

                        // TFORLOOP block detection
                        if (this.Lines[i - 1].Instr.OpCode == LuaOpcode.TFORLOOP)
                        {
                            var loopStart = this.Lines[i - 1 + this.Lines[i].Instr.sBx];
                            loopStart.Op3 += $"\n\r{new string('\t', loopStart.Depth)}for {this.Lines[i - 1].Op2} in {this.Lines[i - 1].Op3} do";
                            this.Lines[i - 1].Op1 = "end";
                            this.Lines[i - 1].Op2 = ""; // erase
                            this.Lines[i - 1].Op3 = "";
                        }

                        // IF's END detection
                        switch (this.Lines[i - 1].Instr.OpCode)
                        {
                            case LuaOpcode.EQ: 
                            case LuaOpcode.LT:
                            case LuaOpcode.LE: // if start detected
                                break;
                            default: // no if start detected
                                this.Lines[i + 1 + (short)this.Lines[i].Instr.sBx].Op1 = "end\n\r" + this.Lines[i + 1 + (short)this.Lines[i].Instr.sBx].Op1;
                                break;
                        }
                        */
                        break;
                    case LuaOpcode.EQ:
                    case LuaOpcode.LT: // NOTE: this is taken care of on JMP
                    case LuaOpcode.LE:
                        /*
                        // NOTE: EQ, LT, LE also increase the PC when true to avoid the first JMP
                        //       which leads to the second part of the if statement
                        //if (this.Lines[i + 1].Instr.OpCode == LuaOpcode.JMP)
                        //{
                        //    // else or end
                        //    string keyword = $"end\n\r{new string('\t', this.Lines[i].Depth)}";
                        //    if (this.Lines.Count > (i + 2 + (short)(this.Lines[i + 1].Instr.sBx)))
                        //    {
                        //        switch (this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Instr.OpCode)
                        //        {
                        //            case LuaOpcode.EQ:
                        //            case LuaOpcode.LT:
                        //            case LuaOpcode.LE: // another if for elseif
                        //                keyword = "else";
                        //                break;
                        //        }
                        //    }
                        //    if (this.Lines[i + 1 + (short)(this.Lines[i + 1].Instr.sBx)].Instr.OpCode == LuaOpcode.JMP)
                        //        keyword = $"else\n\r{new string('\t', this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Depth)}"; // JMP indicates there is another block
                        //    this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Op1 = keyword + this.Lines[i + 2 + (short)(this.Lines[i + 1].Instr.sBx)].Op1;
                        //}
                        // pre IF instruction merg
                        if(i > 2)
                            switch (this.Lines[i -2].Instr.OpCode)
                            {
                                case LuaOpcode.EQ:
                                case LuaOpcode.LT:
                                case LuaOpcode.LE:
                                    this.Lines[i].Op1 = ""; // remove 'if'
                                    break;
                            }
                        // post IF instruction merge
                        switch (this.Lines[i + 2].Instr.OpCode)
                        {
                            case LuaOpcode.EQ:
                            case LuaOpcode.LT:
                            case LuaOpcode.LE:
                                if((short)this.Lines[i + 1].Instr.sBx - (short)this.Lines[i + 3].Instr.sBx == 2) // if next to each other then OR
                                    this.Lines[i].Op3 = "and"; // define or/and
                                else
                                    this.Lines[i].Op3 = "or"; // OR if they point to the same location
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
                        */
                        break;
                    // TODO: remove this to another level?
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

        private void FixCodeBlocks()
        {
            // iterate to define block start/end
            for (int i = 0; i < this.Lines.Count; i++)
            {
                switch (this.Lines[i].Instr.OpCode)
                {
                    case LuaOpcode.FORLOOP:
                        this.Lines[i].Op1 = "end -- FORLOOP\n\r" + this.Lines[i].Op1;
                        break;
                    case LuaOpcode.JMP:
                        //this.Lines[i + 1 + this.Lines[i].Instr.sBx].Op1 = "." + this.Lines[i + 1 + this.Lines[i].Instr.sBx].Op1;
                        
                        ////TODO:
                        ////if(this.Lines[i].Instr.sBx == 1)
                        ////{
                        ////    // JMP 1 commonly used for lots of things
                        ////    this.Lines[i + 1].Op3 += "\r\nend.";
                        ////}
                        ////else
                            this.Lines[i + 1 + this.Lines[i].Instr.sBx].BranchInc.Add(i); // let em know incomming jumps

                        break;
                    case LuaOpcode.RETURN:
                        if(i == this.Lines.Count-1)
                        {
                            this.Lines[i].Op1 = "end";
                            this.Lines[i].Op2 = "";
                            this.Lines[i].Op3 = "";
                        }
                        break;
                }
            }

            // iterate to name blocks
            for (int i = 0; i < this.Lines.Count; i++)
            {
                if(this.Lines[i].BranchInc.Count > 0)
                {
                    if(this.Lines[i].BranchInc.Count > 1) // NOTE: Skip when more then 1 jump, probs indicates an end anyway
                    {
                        ////TODO: this.Lines[i].Op1 = "end\n\r" + this.Lines[i].Op1;
                        continue;
                    }

                    for(int j = 0; j < this.Lines[i].BranchInc.Count; j++)
                    {
                        if(this.Lines[i-1].Instr.OpCode == LuaOpcode.JMP)
                        {
                            if(this.Lines[i].Instr.OpCode == LuaOpcode.LE || this.Lines[i].Instr.OpCode == LuaOpcode.LT
                                || this.Lines[i].Instr.OpCode == LuaOpcode.EQ || this.Lines[i].Instr.OpCode == LuaOpcode.TESTSET
                                || this.Lines[i].Instr.OpCode == LuaOpcode.TEST )
                            {
                                this.Lines[i].Op1 = "else" + this.Lines[i].Op1;
                            }else
                                this.Lines[i].Op1 = "_ " + this.Lines[i].Op1;
                            continue;
                        }
                        // check if IF statements is next to IF
                        if(this.Lines[this.Lines[i].BranchInc[j] - 1].IsCondition())
                        {
                            // IF confirmd
                            //this.Lines[i].Op1 = "if " + this.Lines[i].Op1;
                            //this.Lines[this.Lines[i].BranchInc[j]].Op1 = "IF_ " + this.Lines[this.Lines[i].BranchInc[j]].Op1;
                        }
                        else if(this.Lines[this.Lines[i].BranchInc[j] - 1].IsBranch())
                        {
                            this.Lines[i].Op1 = "else" + this.Lines[i].Op1;
                            break;
                        }   
                    }
                }
            }

            // post process IF mergings
            for (int i = 2; i < this.Lines.Count-1; i++)
            {
                if (this.Lines[i].IsCondition())
                {
                    // Do Right-side
                    int forwardIf = -1;
                    for(int j = i+2; j < this.Lines.Count-1; j++)
                    {
                        if(this.Lines[j].IsCondition())
                        {
                            forwardIf = j; // set target
                            break;
                        }
                        if (!this.Lines[j].IsMove())
                            break; // fuck you
                    }
                    if (this.Lines[i+1].IsBranch() && forwardIf != -1)
                    {
                        // replace 'then' with keyword
                        if ((forwardIf+1) + (short)this.Lines[forwardIf+1].Instr.sBx == (i+1) + (short)this.Lines[i +1].Instr.sBx) // if the point to same result its or
                            this.Lines[i].Op3 = "or";
                        else
                            this.Lines[i].Op3 = "and";
                    }

                    // Do Left-side
                    int backwardId = -1;
                    for (int j = i - 1; j > 2; j--)
                    {
                        if (this.Lines[j].IsBranch())
                        {
                            backwardId = j; // set target
                            break;
                        }
                        if (!this.Lines[j].IsMove())
                            break; // fuck you
                    }
                    if (backwardId != -1)
                    {
                        this.Lines[i].Op1 = "";
                    }
                }
            }

            // IF end blocks
            for (int i = 0; i < this.Lines.Count-1; i++)
            {
                if (!this.Lines[i].IsCondition())
                    continue;
                if (!this.Lines[i + 1].IsBranch())
                    continue;
                int targetJmp = this.Lines[i + 1].Instr.sBx + i + 1;
                if (!this.Lines[targetJmp-1].IsCondition() && !this.Lines[targetJmp].IsBranch()) // dont end when there is a jump
                    this.Lines[this.Lines[i + 1].Instr.sBx].Op1 += " --end ";
                    //this.Lines[this.Lines[i + 1].Instr.sBx].Op1 = "end\n\r" + this.Lines[this.Lines[i + 1].Instr.sBx].Op1;
            }
        }

        private void Realign()
        {
            // text based because we did wanky things instead of respecting the list
            _text = GetText();
            int tabCount = 1;
            string[] lines = Text.Replace("\r", "").Replace("\t", "").Split('\n');
            string newText = "";
            for (int i = 0; i < lines.Length; i++)
            {
                //string[] moreLines = lines[i].Split(';');
                bool postAdd = false;
                bool postSub = false;
                if (lines[i].StartsWith("if") || lines[i].StartsWith("function") || lines[i].StartsWith("for"))
                    postAdd = true;
                else if (lines[i].StartsWith("else"))
                {
                    if (i < lines.Length - 1 && lines[i + 1].StartsWith("if"))
                    {
                        // elseif
                        newText += $"{new string('\t', tabCount - 1)}{lines[i]}{lines[i + 1]}\n\r";
                        i += 1; // brrrr fuck y'all, i skip next one this way!
                        continue;
                    }
                    else
                    {
                        // else
                        tabCount -= 1;
                        postAdd = true;
                    }
                }
                else if (lines[i].StartsWith("end"))
                    tabCount -= 1;

                if (tabCount < 0)
                    tabCount = 0;

                if (lines[i].StartsWith("if"))
                    newText += $"{new string('\t', tabCount)}{lines[i]}";
                else if (lines[i].EndsWith("or") || lines[i].EndsWith("and") || lines[i].StartsWith(" not"))
                    newText += $"{lines[i]}";
                else
                    newText += $"{new string('\t', tabCount)}{lines[i]}\n\r";

                if (lines[i].EndsWith("then"))
                    newText += "\n\r";

                if (postAdd)
                    tabCount += 1;
                if (postSub)
                    tabCount -= 1;
            }
            _text = newText;
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
