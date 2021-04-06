using LuaToolkit.Core;
using LuaToolkit.Disassembler;
using LuaToolkit.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LuaToolkit.Decompiler
{
    public class LuaScriptFunction
    {
        public int Depth;
        private LuaDecoder Decoder;
        private LuaFunction Func;

        public bool IsLocal = false;
        public bool HasVarargs = false;
        private List<int> Args;
        private List<string> NameArgs;
        public List<LuaScriptLine> Lines;

        public List<LuaScriptBlock> Blocks;
        public List<int> UsedLocals;

        private string _text;

        public string Name
        {
            get
            {
                if (this.Func == null || this.Func.Name == null || this.Func.Name == "" || this.Func.Name.Contains("@"))
                {
                    return GetName();
                }
                else
                    return this.Func.Name;
            }
            set
            {
                if (this.Func != null && this.Func.Name != null)
                    this.Func.Name = value;
            }
        }

        public string Text
        {
            get { return GetText(); }
        }

        public LuaScriptFunction(string name, int argsCount, LuaFunction func, LuaDecoder decoder)
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
            HandleUpvalues(); // get upvalues from parent TODO: Bugfix
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
            string args = "(";
            for (int i = 0; i < this.NameArgs.Count; i++)
            {
                args += this.NameArgs[i];
                if (i < this.NameArgs.Count - 1 || this.HasVarargs)
                    args += ", ";
            }
            args += (this.HasVarargs ? "...)" : ")");
            return (this.IsLocal ? "local " : "") + $"function {GetName()}{args}\r\n";
        }

        private string GetName()
        {
            if (this.Func.Name == "" || this.Func.Name.Contains("@")) // unknownX
            {
                // TODO: prefix functions so we can distiguins one parent from another? (like: unknown_0_1)
                var parent = GetParentFunction();
                if (parent == null)
                    return "unkErr";

                // TODO: get all parents?
                int unkCount = -1;
                for (int i = 0; i < parent.Functions.IndexOf(this.Func); i++)
                {
                    if (parent.Functions[i].ScriptFunction.IsLocal)
                        unkCount++;
                }
                return "unknown" + (unkCount + 1); // should give right index?
            }
            return this.Func.Name;
        }

        public LuaFunction GetParentFunction()
        {
            if (this.Decoder.File.Function == this.Func)
                return null; // we root already

            return FindParentFunction(this.Decoder.File.Function);
        }

        private LuaFunction FindParentFunction(LuaFunction function, LuaFunction search = null)
        {
            // NOTE: recursive, always nice to stackoverflow

            // check if any the functions matches us
            if (search == null)
                search = this.Decoder.File.Function;

            var target = search.Functions.FindIndex(x => x.ScriptFunction == this);
            if (target != -1)
                return search;

            // search children
            foreach (var f in search.Functions)
            {
                var res = FindParentFunction(function, f);
                if (res != null)
                    return res;
            }

            return null;
        }

        // NOTE: Please do NOT touch this unless you 110% know what you are doing!!!
        private void GenerateBlocks(bool overwriteBlocks = false)
        {
            int index = 0;
            if (overwriteBlocks || this.Blocks.Count == 0)
            {
                this.Blocks.Clear();
                while (index < this.Lines.Count)
                {
                    LuaScriptBlock b = new LuaScriptBlock(index, this.Decoder, this.Func);
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

                BlockSplitLines = BlockSplitLines.OrderBy(x => x.Value).ToList(); // important to sort by lineNumber or other blocks wont get done otherwise
                                                                                  // cut blocks and make new ones
                for (int i = 0; i < BlockSplitLines.Count; i++)
                {
                    if (this.Blocks[BlockSplitLines[i].Key].StartAddress + this.Blocks[BlockSplitLines[i].Key].Lines.Count < BlockSplitLines[i].Value ||
                        this.Blocks[BlockSplitLines[i].Key].StartAddress > BlockSplitLines[i].Value)
                        continue; // already circumcised this boi

                    if (this.Blocks.Find(x => x.StartAddress == BlockSplitLines[i].Value) != null)
                        continue; // been there, done that

                    LuaScriptBlock splitBlock = new LuaScriptBlock(BlockSplitLines[i].Value, this.Decoder, this.Func);
                    for (int j = BlockSplitLines[i].Value - this.Blocks[BlockSplitLines[i].Key].StartAddress; j < this.Blocks[BlockSplitLines[i].Key].Lines.Count; j++)
                        splitBlock.Lines.Add(this.Blocks[BlockSplitLines[i].Key].Lines[j]); // copy from old to new

                    // delete old lines
                    if (splitBlock.Lines.Count > 0)
                        this.Blocks[BlockSplitLines[i].Key].Lines.RemoveRange(BlockSplitLines[i].Value - this.Blocks[BlockSplitLines[i].Key].StartAddress, splitBlock.Lines.Count);

                    this.Blocks.Insert(BlockSplitLines[i].Key + 1, splitBlock); // insert new block after modified one
                                                                                // update BlockSplitLines indexing
                    for (int j = i + 1; j < BlockSplitLines.Count; j++)
                        BlockSplitLines[j] = new KeyValuePair<int, int>(BlockSplitLines[j].Key + 1, BlockSplitLines[j].Value); // offset remaining blocks
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
                                this.Blocks[i].JumpsNext = this.Blocks[i + 1].StartAddress;
                                break; // jmp?
                                       //case LuaOpcode.LOADBOOL: // pc++
                                       //    this.Blocks[i].JumpsTo = (this.Blocks[i].StartAddress + this.Blocks[i].Lines.Count - 1) + 2; // skips one if C
                                       //    this.Blocks[i].JumpsNext = (this.Blocks[i].StartAddress + this.Blocks[i].Lines.Count - 1) + 1; // next block
                                       //    break;
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
                                    this.Blocks[i].JumpsNext = -1; // this.Blocks[i + 1].StartAddress;
                                }
                                break;
                            default:
                                this.Blocks[i].JumpsTo = -1; // erase from possible previous block?
                                this.Blocks[i].JumpsNext = this.Blocks[i + 1].StartAddress;
                                break;
                        }
                    }
                }
            }

            for (int i = 0; i < this.Blocks.Count; i++)
            {
                // IF: JMP != -1 && ELSE != -1 (&& GetConditionLine != NULL; ELSE; FORLOOP END (dont care))
                // ELSE: JMP != -1 && ELSE == -1
                // ENDIF: JMP == -1 && ELSE != -1
                // END: JMP == -1 && ELSE == -1
                if (this.Blocks[i].GetBranchLine() != null &&
                    (this.Blocks[i].GetBranchLine().Instr.OpCode == LuaOpcode.FORLOOP || this.Blocks[i].GetBranchLine().Instr.OpCode == LuaOpcode.TFORLOOP))
                {
#if DEBUG
                    this.Blocks[i].GetBranchLine().Text = "end -- ENDLOOP\r\n";
#else
                    this.Blocks[i].GetBranchLine().Text = "end\r\n";
#endif
                }
                else if (this.Blocks[i].JumpsTo != -1 && this.Blocks[i].JumpsNext != -1 && this.Blocks[i].GetConditionLine() != null) // IF detected
                {
                    try
                    {
                        // merge
                        int lastifIndex = -1;
                        int bIndex = i + 1; // search end of IF
                        while (bIndex < this.Blocks.Count)
                        {
                            if (this.Blocks[bIndex].JumpsTo == -1 || this.Blocks[bIndex].JumpsNext == -1)
                            {
                                lastifIndex = bIndex - 1;
                                break; // IF found
                            }
                            bIndex++;
                        }
                        // TODO: find the bodyblock for the last IF and compare against previous IF to find merge chain,
                        // re-do group IF's that do not match the ifbodyblock end/start-1 and figure out if its and/or 
                        // depending on where the jump is set to. The last one should always be classified as 'and'
                        // can be used to figure out the END of the ifbodyblock, we check others by keeping in mind
                        // they can be both and/or, meaning ifbodyblock (and) || ifbodyblock-1 (or)

                        // iterate from lastifIndex to i and split
                        int ifIndex = lastifIndex;
                        if (this.Blocks[i].IfChainIndex != -1)
                            continue; // skip if already discovered

                        while (ifIndex >= i)
                        {
                            // NOTE: not always the case??
                            // TODO: INF loop somtimes!!
                            var ifbodyBlockEnd = this.Blocks.ToList().Single(x => x.StartAddress == this.Blocks[lastifIndex].JumpsTo); // end JMP
                            var ifbodyBlockStart = this.Blocks[lastifIndex + 1]; // start +1

                            // NOTE: iterate from end to here to which block it JMPs to
                            bool found = false;
                            int cIndex = this.Blocks.IndexOf(ifbodyBlockEnd); // start from endblock
                            while (cIndex > ifIndex)
                            {
                                // scan if's (and ONLT if's)
                                if (this.Blocks[ifIndex].JumpsTo == this.Blocks[cIndex].StartAddress 
                                    && this.Blocks[ifIndex].GetConditionLine() != null && this.Blocks[ifIndex].GetConditionLine().IsCondition())
                                {
                                    found = true;
                                    bool jmpsToStart = this.Blocks[ifIndex].JumpsTo == ifbodyBlockStart.StartAddress; // is or?
                                    if (jmpsToStart)
                                    {
                                        if (ifIndex != lastifIndex)
                                            this.Blocks[ifIndex].GetConditionLine().Op3 = "or";
                                        if (this.Blocks[ifIndex].GetConditionLine().Instr.A == 0)
                                            this.Blocks[ifIndex].GetConditionLine().Op2 = this.Blocks[ifIndex].GetConditionLine().Op2.Replace("==", "~=");
                                    }
                                    else
                                    {
                                        if (ifIndex != lastifIndex)
                                            this.Blocks[ifIndex].GetConditionLine().Op3 = "and";
                                        if (this.Blocks[ifIndex].GetConditionLine().Instr.A == 1)
                                            this.Blocks[ifIndex].GetConditionLine().Op2 = this.Blocks[ifIndex].GetConditionLine().Op2.Replace("==", "~=");
                                    }

                                    if (ifIndex != lastifIndex && this.Blocks[ifIndex + 1].GetConditionLine() != null)
                                        this.Blocks[ifIndex + 1].GetConditionLine().Op1 = "";  
                                    if (this.Blocks[ifIndex].IfChainIndex == -1)
                                        this.Blocks[ifIndex].IfChainIndex = ifIndex - i; // NOTE: numbers are NOT correct after rebase!

                                    break;
                                }
                                cIndex--;
                            }
                            ifIndex--;
                            if (!found)
                            {
                                ifIndex++;
                                if (this.Blocks[ifIndex + 1].IfChainIndex != -1)
                                {
                                    // cleanup existing end of merged ifchain
                                    int ifIndexFix = ifIndex + 1;
                                    do
                                    {
                                        this.Blocks[ifIndexFix].IfChainIndex = ifIndexFix - ifIndex - 1; // rebase
                                        ifIndexFix++;
                                    }
                                    while (this.Blocks[ifIndexFix].IfChainIndex != -1);
                                }

                                lastifIndex = ifIndex; // new IF end found!

                                // TODO: this below shit is very buggy, temp fix for FORLOOPs?
                                if(cIndex < ifIndex || this.Blocks[ifIndex].GetConditionLine() == null) // skip LOOPs?
                                    ifIndex--; // subtract or inf loop?? TODO: bugfix, may 
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else if (this.Blocks[i].JumpsTo != -1 && this.Blocks[i].JumpsNext == -1)
                {
#if DEBUG
                    this.Blocks[i].GetBranchLine().Postfix += "else -- ELSE";
#else
                    this.Blocks[i].GetBranchLine().Postfix += "else";
#endif
                }
                else if (this.Blocks[i].JumpsTo == -1 && this.Blocks[i].JumpsNext != -1 && this.Blocks[i].GetBranchLine() != null
                    && this.Blocks[i].GetBranchLine().Instr.OpCode != LuaOpcode.FORPREP) // also make sure if condifition is set (no forloop)
                {

#if DEBUG
                    this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Postfix += "\r\nend -- ENDIF";
#else
                    this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Postfix += "\r\nend";
#endif
                }

                else if (this.Blocks[i].JumpsTo == -1 && this.Blocks[i].JumpsNext == -1)
                {
#if DEBUG
                    this.Blocks[i].GetBranchLine().Postfix += " -- END\r\n"; // already taken care of
#endif
                }
            }
        }

        // NOTE: OO?
        public string GetConstant(int index, LuaFunction targetFunc = null)
        {
            if (targetFunc == null)
                targetFunc = this.Func; // self
            if (index > 255 && targetFunc.Constants[index - 256] != null)
                return targetFunc.Constants[index - 256].ToString();
            else if (targetFunc.Constants.Count > index)
                return targetFunc.Constants[index].ToString();
            else
                return "unk" + index;
        }

        private void HandleUpvalues()
        {
            // NOTE: Upvalues are used for function prototypes and are referenced to in a global scope
            // My job is to generate a list of static available upvalues so that the decompiler can
            // reference to them, they are commonly used for function calls that are in scope of the root function.

            //return;
            LuaFunction parent = GetParentFunction();
            if (parent == null)
                return; // we in root UwU

            //return;
            // create Upvalues List from parent
            int functionIndex = parent.Functions.IndexOf(this.Func);
            for (int i = 0; i < parent.Instructions.Count; i++)
            {
                // TODO: bugfix
                var instr = parent.Instructions[i];
                if (!(instr.OpCode == LuaOpcode.CLOSURE && instr.Bx == functionIndex))
                    continue;

                string globalName = "";
                this.IsLocal = true;
                int j = i - 1;
                // Find GETGLOBAL - if needed?
                //while (j >= 0)
                //{
                //    if (parent.Instructions[j].OpCode == LuaOpcode.GETGLOBAL && parent.Instructions[i].A == parent.Instructions[j].A)
                //    {
                //        globalName = parent.Constants[parent.Instructions[j].Bx].ToString();
                //        globalName = globalName.Substring(1, globalName.Length - 2);
                //        break; // job's done
                //    }
                //    j--;
                //}

                j = i + 1; // instr after CLOSURE to start with
                bool closure = false;
                int setTableIndex = -1;
                while (j < parent.Instructions.Count)
                {
                    if (parent.Instructions[j].OpCode == LuaOpcode.CLOSURE || parent.Instructions[j].OpCode == LuaOpcode.CLOSE || parent.Instructions[j].OpCode == LuaOpcode.RETURN)
                        break; // end of closure
                    //closure = true; // stop MOVEs after closure, keep going for settable/setglobal


                    if (parent.Instructions[j].OpCode == LuaOpcode.MOVE && !closure)
                    {
                        // upvalues!
                        if (parent.Instructions[j].A == 0) // 0 = _ENV
                        {
                            // TODO: handle value correct & erase script line
                            LuaConstant cons;
                            string obj = GetConstant(parent.Instructions[j].B, parent).ToString();
                            //if(parent.ScriptFunction != null)
                            //    obj = parent.ScriptFunction.Lines.FirstOrDefault().WriteIndex(parent.Instructions[j].B);
                            //if (!obj.Contains("var"))
                            //    cons = new PrototypeConstant($"{parent.Name}_{parent.Instructions[j].B}\0"); // TODO: parent name or actual name?
                            //else
                            //    cons = new StringConstant(obj); // idk?
                            if(obj.Contains('\"'))
                                cons = new StringConstant(obj.Substring(1, obj.Length-2) + "\0");
                            else
                                cons = new StringConstant(obj + "\0");
                            this.Func.Upvalues.Add(cons);
                        }
                    }
                    else if (parent.Instructions[j].OpCode == LuaOpcode.SETTABLE)
                    {
                        // check the source and desitnation of the SETTABLE to find out both local and global name
                        if (setTableIndex == -1 && parent.Instructions[i].A == parent.Instructions[j].C) // SETTABLE x y == CLOSURE y ?  
                        {
                            // find first part of the table

                            // TODO: bugfix false locals
                            this.IsLocal = false;
                            this.Name = GetConstant(parent.Instructions[j].B, parent).ToString();
                            this.Name = this.Name.Substring(1, this.Name.Length - 2);
                            //closure = true;
                            setTableIndex = j; // src
                        }
                        else if (setTableIndex > -1 && parent.Instructions[setTableIndex].A == parent.Instructions[j].C)
                        {
                            // find second part of the table, which is the root/global
                            globalName = GetConstant(parent.Instructions[j].B, parent).ToString();
                            //globalName = globalName.Substring(1, globalName.Length - 2);
                            //break;
                        }
                    }
                    else if (parent.Instructions[j].OpCode == LuaOpcode.SETGLOBAL && !closure && parent.Instructions[i].A == parent.Instructions[j].A) // CLOSURE x ? == SETGLOBAL x ?
                    {
                        // is global!
                        this.IsLocal = false;
                        this.Name = GetConstant(parent.Instructions[j].C, parent).ToString();
                        this.Name = this.Name.Substring(1, this.Name.Length - 2);
                        //closure = true;
                        //break;
                    }
                    j++;
                }
                if (globalName != "")
                    this.Name = globalName + ":"+ this.Name;

                // set line CLOSURE from parent
                parent.ScriptFunction.Lines[i].SetFunctionRef(this.Func);
            }
        }

        private void HandleTailcallReturns()
        {
            // NOTE: Tailcalls have 2 returns when C functions or 1 when Lua functions
            // My job is to remove those RETURNS because they are only used in the Lua VM
            for (int i = 0; i < this.Blocks.Count; i++)
            {
                for (int j = 0; j < this.Blocks[i].Lines.Count; j++)
                {
                    if (this.Blocks[i].Lines[j].Instr.OpCode == LuaOpcode.RETURN)
                    {
                        // check if previous 1/2 is a TAILCALL
                        bool erase = false;
                        if (j >= 1 && this.Blocks[i].Lines[j - 1].Instr.OpCode == LuaOpcode.TAILCALL)
                            erase = true;
                        else if (j >= 2 && this.Blocks[i].Lines[j - 2].Instr.OpCode == LuaOpcode.TAILCALL)
                            erase = true;

                        if (erase)
                        {
#if DEBUG
                            this.Blocks[i].Lines[j].Op1 = "-- TAILCALL RETURN"; // erase keyword
#else
                            this.Blocks[i].Lines[j].Op1 = ""; // erase keyword
#endif
                            this.Blocks[i].Lines[j].Op2 = ""; // erase variables
                            //this.Blocks[i].Lines[j].Op3 = ""; // erase (dont erase this, contains else)
                        }
                    }
                }
            }
        }

        private void OutlineConditions()
        {
            // NOTE: IF statements may have more then 2 instruction (IF, JMP) when they are chained
            // My job is to optimize those merged IF blocks so that inline IFs are working fine
            // EDIT: nvm I just tweak them and move the instructions that arent IF/JMP up so the merge is clean
            foreach (var b in this.Blocks)
                if (b.GetConditionLine() != null && b.IfChainIndex > 0)
                    b.Optimize();
        }

        private void UpdateClosures()
        {
            // NOTE: update the names of the functions
            for(int i = 0; i < this.Lines.Count; i++)
            {
                if (this.Lines[i].Instr.OpCode != LuaOpcode.CLOSURE)
                    continue;

                if (this.Func.Functions[this.Lines[i].Instr.Bx].ScriptFunction != null)
                    this.Lines[i].Op3 = this.Func.Functions[this.Lines[i].Instr.Bx].ScriptFunction.Name;

            }
        }

        public void Complete(bool overwriteBlocks = false)
        {
            Cleanlines();
            GenerateBlocks(overwriteBlocks);
            UpdateClosures(); // fixes closure name referncing
            HandleTailcallReturns(); // fix returns
            OutlineConditions(); // moves IF code above IF chain
        }

        public string GetText()
        {
            if (this.Blocks.Count == 0)
                this.Complete(); // i guess?

            //if (_text != null)
            //    return _text; // stores end results

            string result = this.ToString();
#if DEBUG
            result += GenerateDebugCode();
#else
            result += GenerateCleanCode();
#endif
            return result;
        }

        private string GenerateDebugCode()
        {
            
            string result = "";
            int tabLevel = 0;
            for (int b = 0; b < this.Blocks.Count; b++)
            {
                // print block content
                for (int i = 0; i < this.Blocks[b].Lines.Count; i++)
                {
                    if (this.Blocks[b].Lines[i].Instr.OpCode == LuaOpcode.CLOSURE)
                        result += this.Blocks[b].Lines[i].GetFunctionRef().ScriptFunction.GetText(); // inline func in parent
                    result += (this.Blocks[b].StartAddress + i).ToString("0000") + $": {new string(' ', tabLevel)}" + this.Blocks[b].Lines[i].Text.Replace("\t", "");
                }
                result += new string('-', 50) + $" ({this.Blocks[b].JumpsTo}) \r\n";
                if (b == this.Blocks.Count - 1)
                    result += "\r\n"; // keep it clean?
            }
            return result;
        }

        private string GenerateCleanCode()
        {
            string result = "";
            for (int b = 0; b < this.Blocks.Count; b++)
            {
                for (int i = 0; i < this.Blocks[b].Lines.Count; i++)
                {
                    if(this.Blocks[b].Lines[i].Instr.OpCode == LuaOpcode.CLOSURE)
                        if (this.Blocks[b].Lines[i].GetFunctionRef() != null)
                            result += this.Blocks[b].Lines[i].GetFunctionRef().ScriptFunction.BeautifieCode(); // inline func in parent
                            //result += this.Blocks[b].Lines[i].FunctionRef.ScriptFunction.RealignText().Replace("\r\n",$"\r\n{new string('\t',1)}"); // inline func in parent
                    result += this.Blocks[b].Lines[i].Text; //.Replace("\t", "");
                }

                if (b == this.Blocks.Count - 1)
                    result += "\n\r"; // keep it clean?
            }
            return result;
        }

        public void Cleanlines()
        {
            for (int i = 0; i < this.Lines.Count; i++)
                this.Lines[i].ClearLine();
        }

        public string BeautifieCode()
        {
            // text based because we did wanky things instead of respecting the list	
            int tabCount = 1;
            string[] lines = Text.Replace("\r", "").Replace("\t", "").Split('\n');
            string newText = "";
            for (int i = 0; i < lines.Length; i++)
            {
                bool postAdd = false;
                bool postSub = false;
                if (lines[i].StartsWith("if") || lines[i].StartsWith("function") || lines[i].StartsWith("local function") || lines[i].StartsWith("for"))
                    postAdd = true;
                else if (lines[i].StartsWith("else"))
                {
                    if (i < lines.Length - 1 && lines[i + 1].StartsWith("if"))
                    {
                        // elseif	
                        newText += $"{new string('\t', tabCount)}{lines[i]}{lines[i + 1]}\r\n";
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
                else if (lines[i] == "")
                    newText += "";
                else
                    newText += $"{new string('\t', tabCount)}{lines[i]}\r\n";

                if (lines[i].EndsWith("then"))
                    newText += "\r\n";

                if (postAdd)
                    tabCount += 1;
                if (postSub)
                    tabCount -= 1;
            }
            return newText;
        }
    }

}
