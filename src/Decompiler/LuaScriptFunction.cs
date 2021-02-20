﻿using System;
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
        public bool IsLocal = false;
        private List<int> Args;
        private List<string> NameArgs;
        public List<LuaScriptLine> Lines;

        public List<LuaScriptBlock> Blocks;
        public List<int> UsedLocals;

        private string _text;

        public string Text
        {
            get { return GetText(); }
        }

        public LuaScriptFunction(string name, int argsCount, ref LuaFunction func, ref LuaDecoder decoder)
        {
            this.Name = name;
            this.Func = func;
            this.Func.ScriptFunction = this; // reference this for lateron
            this.Decoder = decoder;
            this.Lines = new List<LuaScriptLine>();
            this.Blocks = new List<LuaScriptBlock>();
            this.UsedLocals = new List<int>();
            InitArgs(argsCount);
            this.UsedLocals.AddRange(this.Args);
            HandleUpvalues(); // get upvalues from parent
        }

        private void InitArgs(int count)
        {
            this.Args = new List<int>();
            this.NameArgs = new List<string>();
            for (int i = 0; i < count; i++)
            {
                this.Args.Add(i);
                this.NameArgs.Add($"var{i}");
            }
        }

        public override string ToString()
        {
            if (this.Name == null && GetName() != null)
                return "-- root file\n\r";

            string args = "(";
            for (int i = 0; i < this.NameArgs.Count; i++)
            {
                args += this.NameArgs[i];
                if (i < this.NameArgs.Count - 1)
                    args += ", ";
            }
            args += ")";
            return (this.IsLocal ? "local " : "") + $"function {GetName()}{args}\n\r";
        }

        private string GetName()
        {
            if (Func.Name != null && Func.Name != "")
                return Func.Name;

            if (this.Name == "") // unknownX
            {
                // TODO: prefix functions so we can distiguins one parent from another? (like: unknown_0_1)
                var parent = GetParentFunction();
                // TODO: get all parents?
                int unkCount = -1;
                for(int i = 0; i < parent.Functions.IndexOf(this.Func); i++)
                {
                    if (parent.Functions[i].ScriptFunction.IsLocal)
                        unkCount++;
                }
                return "unknown" + (unkCount + 1); // should give right index?
            }
            return this.Name;
        }

        public LuaFunction GetParentFunction()
        {
            if(this.Decoder.File.Function == this.Func)
                return null; // we root already

            return FindParentFunction(this.Decoder.File.Function);
        }

        private LuaFunction FindParentFunction(LuaFunction function)
        {
            // NOTE: recursive, always nice to stackoverflow

            // check if any the functions matches us
            var target = function.Functions.IndexOf(this.Func);
            if (target != -1)
                return function;
            else if (function == this.Func)
                return null; // returns itself?

            // no match? continue search
            if (function.Functions.Count > 0)
            {
                var res = FindParentFunction(function);
                if (res != null)
                    return res; // parent
            }
            return null;
        }


        private void GenerateBlocks()
        {
            int index = 0;
            this.Blocks.Clear();
            while (index < this.Lines.Count)
            {
                LuaScriptBlock b = new LuaScriptBlock(index, ref this.Decoder, ref this.Func);
                while (index < this.Lines.Count)
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
                if (targets.Count > 0)
                    BlockSplitLines.Add(new KeyValuePair<int, int>(this.Blocks.FindIndex(x => x.StartAddress == targets[0].StartAddress), this.Blocks[i].JumpsTo));
                //foreach (var tb in targets)
                //    BlockSplitLines.Add(new KeyValuePair<int, int>(this.Blocks.FindIndex(x => x.StartAddress == tb.StartAddress), i));
            }

            BlockSplitLines = BlockSplitLines.OrderBy(x => x.Key).ToList();
            // cut blocks and make new ones
            for (int i = 0; i < BlockSplitLines.Count; i++)
            {
                if (this.Blocks[BlockSplitLines[i].Key].StartAddress + this.Blocks[BlockSplitLines[i].Key].Lines.Count < BlockSplitLines[i].Value ||
                    this.Blocks[BlockSplitLines[i].Key].StartAddress > BlockSplitLines[i].Value)
                    continue; // already circumcised this boi

                if (this.Blocks.Find(x => x.StartAddress == BlockSplitLines[i].Value) != null)
                    continue; // been there, done that

                LuaScriptBlock splitBlock = new LuaScriptBlock(BlockSplitLines[i].Value, ref this.Decoder, ref this.Func);
                for (int j = BlockSplitLines[i].Value - this.Blocks[BlockSplitLines[i].Key].StartAddress; j < this.Blocks[BlockSplitLines[i].Key].Lines.Count; j++)
                    splitBlock.Lines.Add(this.Blocks[BlockSplitLines[i].Key].Lines[j]); // copy from old to new

                // delete old lines
                if (splitBlock.Lines.Count > 0)
                    this.Blocks[BlockSplitLines[i].Key].Lines.RemoveRange(BlockSplitLines[i].Value - this.Blocks[BlockSplitLines[i].Key].StartAddress, splitBlock.Lines.Count);

                this.Blocks.Insert(BlockSplitLines[i].Key + 1, splitBlock); // insert new block after modified one
                // update BlockSplitLines indexing
                for (int j = i + 1; j < BlockSplitLines.Count; j++)
                    BlockSplitLines[j] = new KeyValuePair<int, int>(BlockSplitLines[j].Key + 1, BlockSplitLines[j].Value); // offset
            }

            // fix JumpsTo and JumpsNext ?
            this.Blocks.OrderBy(x => x.StartAddress);
            for (int i = 0; i < this.Blocks.Count; i++)
            {
                // last return
                if (i == this.Blocks.Count - 1)
                {
                    // Last block shouldnt jump to anywhere
                    this.Blocks[i].JumpsTo = -1;
                    this.Blocks[i].JumpsNext = -1;
                    if (this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Instr.OpCode == LuaOpcode.RETURN)
                        this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Op1 = "end"; // replace last RETURN with END
                    continue;
                }
                // Conditions without JMP
                if (this.Blocks[i].Lines.Count > 0)
                {
                    switch (this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Instr.OpCode)
                    {
                        // TODO: check which instructions dont pick the next one
                        case LuaOpcode.TFORLOOP:
                        case LuaOpcode.FORLOOP: // calculate LOOP jump
                            this.Blocks[i].JumpsTo = (this.Blocks[i].StartAddress + this.Blocks[i].Lines.Count - 1) + (short)this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Instr.sBx + 1; // TODO: verify math
                            this.Blocks[i].JumpsNext = this.Blocks[i].JumpsNext = this.Blocks[i + 1].StartAddress;
                            break; // jmp?
                        case LuaOpcode.LOADBOOL: // pc++
                            this.Blocks[i].JumpsTo = (this.Blocks[i].StartAddress + this.Blocks[i].Lines.Count - 1) + 2; // skips one if C
                            this.Blocks[i].JumpsNext = (this.Blocks[i].StartAddress + this.Blocks[i].Lines.Count - 1) + 1; // next block
                            break;
                        case LuaOpcode.JMP: // pc++
                            // check previous condition
                            if (this.Blocks[i].Lines.Count > 1 && this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 2].IsCondition()) // check for IF
                            {
                                // if/test/testset
                                this.Blocks[i].JumpsTo = (this.Blocks[i].StartAddress + this.Blocks[i].Lines.Count - 1) + (short)this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Instr.sBx + 1; // TODO: verify math
                                this.Blocks[i].JumpsNext = this.Blocks[i + 1].StartAddress;
                            }
                            else
                            {
                                // unknown jump
                                this.Blocks[i].JumpsTo = (this.Blocks[i].StartAddress + this.Blocks[i].Lines.Count - 1) + (short)this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Instr.sBx + 1; // TODO: verify math
                                this.Blocks[i].JumpsNext = this.Blocks[i + 1].StartAddress;
                            }
                            break;
                        default:
                            this.Blocks[i].JumpsTo = -1; // erase from possible previous block?
                            this.Blocks[i].JumpsNext = this.Blocks[i + 1].StartAddress;
                            break;
                    }
                }
            }

            // place end in the -1 blocks
            for(int i = 1; i < this.Blocks.Count; i++)
            {
                // EVERY block has and END!
                // place end if i-1 != FORPREP
                // place end at FORLOOP (and TFORLOOP)
                LuaScriptLine pcLine; // previous Condition Line
                LuaScriptLine cLine; // Condition Line
                var bLine = this.Blocks[i].GetBranchLine();
                if(bLine != null)
                    switch (bLine.Instr.OpCode)
                    {
                        case LuaOpcode.JMP: // TODO: fix single line ifs?
                            var dest = this.Blocks.FindIndex(x => x.StartAddress == this.Blocks[i].JumpsTo);
                            //this.Blocks[dest - 1].GetBranchLine().Op3 += " -- END";
                            this.Blocks[dest - 1].GetBranchLine().Op3 += "\r\nend";
                            break;
                        case LuaOpcode.FORLOOP:
                        case LuaOpcode.TFORLOOP:
                            //this.Blocks[dest - 1].GetBranchLine().Op3 += " -- END";
                            this.Blocks[i].GetBranchLine().Op3 += "\r\nend";
                            break;
                    }
            }
        }

        // NOTE: OO?
        private string GetConstant(int index, LuaFunction targetFunc = null)
        {
            if (targetFunc == null)
                targetFunc = this.Func; // self
            if (index > 255 && targetFunc.Constants[index - 256] != null)
                return targetFunc.Constants[index - 256].ToString();
            else
                return targetFunc.Constants[index].ToString();
        }

        private void HandleUpvalues()
        {
            LuaFunction parent = GetParentFunction();
            if (parent == null)
                return; // we in root UwU

            // create Upvalues List from parent
            int functionIndex = parent.Functions.IndexOf(this.Func);
            for (int i = 0; i < parent.Instructions.Count; i++)
            {
                var instr = parent.Instructions[i];
                switch (instr.OpCode)
                {
                    case LuaOpcode.CLOSURE:
                        if (instr.Bx != functionIndex)
                            break;

                        string globalName = "";
                        this.IsLocal = true;
                        int j = i - 1;
                        // Find GETGLOBAL
                        while (j >= 0)
                        {
                            if (parent.Instructions[j].OpCode == LuaOpcode.CLOSURE)
                                break; // start of another closure

                            if (parent.Instructions[j].OpCode == LuaOpcode.GETGLOBAL && parent.Instructions[i].A == parent.Instructions[j].A)
                            {
                                globalName = GetConstant(parent.Instructions[j].B);
                                globalName = globalName.Substring(1, globalName.Length - 2);
                                break; // job's done
                            }
                            j--;
                        }

                        j = i + 1;
                        // Find SETTABLE
                        bool closure = false;
                        int setTableIndex = -1;
                        while (j < parent.Instructions.Count)
                        {
                            if (parent.Instructions[j].OpCode == LuaOpcode.CLOSURE)
                                closure = true; // stop MOVEs after closure, keep going for settable/setglobal


                            if (parent.Instructions[j].OpCode == LuaOpcode.MOVE && !closure)
                            {
                                // upvalues!
                                if (parent.Instructions[j].A == 0) // 0 = _ENV
                                {
                                    // TODO
                                    LuaConstant cons;
                                    cons = new StringConstant($"unknown{parent.Instructions[j].B}\0"); // NOTE: strip last character??
                                    this.Func.Upvalues.Add(cons);
                                }
                            }
                            else if (parent.Instructions[j].OpCode == LuaOpcode.SETTABLE)
                            {
                                // check the source and desitnation of the SETTABLE to find out both local and global name
                                if(setTableIndex == -1 && parent.Instructions[i].A == parent.Instructions[j].C) // SETTABLE x y == CLOSURE y ?  
                                {
                                    // find first part of the table
                                    this.IsLocal = false;
                                    this.Name = GetConstant(parent.Instructions[j].B, parent).ToString();
                                    this.Name = this.Name.Substring(1, this.Name.Length - 2);
                                    closure = true;
                                    setTableIndex = j; // src
                                }
                                else if(setTableIndex > -1 && parent.Instructions[setTableIndex].A == parent.Instructions[j].C)
                                {
                                    // find second part of the table, which is the root/global
                                    globalName = GetConstant(parent.Instructions[j].B, parent).ToString();
                                    globalName = globalName.Substring(1, globalName.Length - 2);
                                    break;
                                }
                            }
                            else if (parent.Instructions[j].OpCode == LuaOpcode.SETGLOBAL && !closure && parent.Instructions[i].A == parent.Instructions[j].A) // CLOSURE x ? == SETGLOBAL x ?
                            {
                                // is global!
                                this.IsLocal = false;
                                this.Name = GetConstant(parent.Instructions[j].C, parent).ToString();
                                this.Name = this.Name.Substring(1, this.Name.Length - 2);
                                closure = true;
                                break;
                            }
                            j++;
                        }

                        if (globalName != "")
                            this.Name = globalName + ":" + this.Name;

                        break;
                    case LuaOpcode.SETUPVAL:
                        // NOTE: check all 'MOV 0 Bx' after CLOSURE & SETUPVALUE
                        // NOTE: these are only used at runtime to set/get values?
                        var test2 = instr.Bx;
                        break;
                }
            }
        }

        public void Complete()
        {
            GenerateBlocks();
            Realign(); // complete?
        }

        public string GetText()
        {
            if (_text != null)
                return _text; // stores end results

            string result = this.ToString();
#if DEBUG
            result += GenerateCode();
#else
            result += GenerateCodeFlat();
#endif
            return result;
        }

        private string GenerateCode()
        {
            string result = "";
            int tabLevel = 0;
            for(int b = 0; b < this.Blocks.Count; b++)
            {
                // print block content
                for(int i = 0; i < this.Blocks[b].Lines.Count; i++)
                    result += (this.Blocks[b].StartAddress + i).ToString("0000") + $": {new string(' ',tabLevel)}" + this.Blocks[b].Lines[i].Text.Replace("\t","");
                
                result += new string('-', 50) + $" ({this.Blocks[b].JumpsTo}) \r\n";
                if (b == this.Blocks.Count - 1)
                    result += "\r\n"; // keep it clean?
            }
            return result;
        }

        private string GenerateCodeFlat()
        {
            string result = "";
            for (int b = 0; b < this.Blocks.Count; b++)
            {
                for (int i = 0; i < this.Blocks[b].Lines.Count; i++)
                {
                    result += this.Blocks[b].Lines[i].Text.Replace("\t","");
                }
               
                if (b == this.Blocks.Count - 1)
                    result += "\n\r"; // keep it clean?
            }
            return result;
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
                if (lines[i].StartsWith("if") || lines[i].StartsWith("function") || lines[i].StartsWith("local function") || lines[i].StartsWith("for"))
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
    }

}
