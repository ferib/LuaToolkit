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
            //HandleUpvalues(); // get upvalues from parent TODO: Bugfix
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

            BlockSplitLines = BlockSplitLines.OrderBy(x => x.Value).ToList(); // important to sort by lineNumber or other blocks wont get done otherwise
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

            // if merge
            for (int i = 0; i < this.Blocks.Count; i++)
            {
                // IF: JMP != -1 && ELSE != -1
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
                else if (this.Blocks[i].JumpsTo != -1 && this.Blocks[i].JumpsNext != -1) // IF detected
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
                        var ifbodyBlockEnd = this.Blocks.ToList().Single(x => x.StartAddress == this.Blocks[lastifIndex].JumpsTo); // end JMP
                        var ifbodyBlockStart = this.Blocks[lastifIndex + 1]; // start +1

                        // NOTE: iterate from end to here to which block it JMPs to
                        bool found = false;
                        int cIndex = this.Blocks.IndexOf(ifbodyBlockEnd); // start from endblock
                        while (cIndex > ifIndex)
                        {
                            // scan if's
                            if (this.Blocks[ifIndex].JumpsTo == this.Blocks[cIndex].StartAddress)
                            {
                                found = true;
                                bool jmpsToStart = this.Blocks[ifIndex].JumpsTo == ifbodyBlockStart.StartAddress; // is or?
                                if(jmpsToStart)
                                {
                                    if (ifIndex != lastifIndex)
                                        this.Blocks[ifIndex].GetConditionLine().Op3 = "or";
                                    if(this.Blocks[ifIndex].GetConditionLine().Instr.A == 0)
                                        this.Blocks[ifIndex].GetConditionLine().Op2 = this.Blocks[ifIndex].GetConditionLine().Op2.Replace("==", "~=");
                                }
                                else
                                {
                                    if(ifIndex != lastifIndex)
                                        this.Blocks[ifIndex].GetConditionLine().Op3 = "and";
                                    if (this.Blocks[ifIndex].GetConditionLine().Instr.A == 1)
                                        this.Blocks[ifIndex].GetConditionLine().Op2 = this.Blocks[ifIndex].GetConditionLine().Op2.Replace("==", "~=");
                                }

                                if (ifIndex != lastifIndex)
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
                                    this.Blocks[ifIndexFix].IfChainIndex = ifIndexFix - ifIndex-1; // rebase
                                    ifIndexFix++;
                                }
                                while (this.Blocks[ifIndexFix].IfChainIndex != -1);
                            }
                                
                            lastifIndex = ifIndex; // new IF end found!
                        } 
                    }
                }
                else if (this.Blocks[i].JumpsTo != -1 && this.Blocks[i].JumpsNext == -1)
                {
#if DEBUG
                    this.Blocks[i].GetBranchLine().Op3 += "else -- ELSE";
#else
                    this.Blocks[i].GetBranchLine().Op3 += "else";
#endif
                }  
                else if (this.Blocks[i].JumpsTo == -1 && this.Blocks[i].JumpsNext != -1 && this.Blocks[i].GetBranchLine() != null
                    && this.Blocks[i].GetBranchLine().Instr.OpCode != LuaOpcode.FORPREP) // also make sure if condifition is set (no forloop)
                {
#if DEBUG
                    this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Op3 += "\r\nend -- ENDIF";
#else
                    this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Op3 += "\r\nend";
#endif
                }
                    
                else if (this.Blocks[i].JumpsTo == -1 && this.Blocks[i].JumpsNext == -1)
                {
#if DEBUG
                    this.Blocks[i].GetBranchLine().Op3 += " -- END\r\n"; // already taken care of
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

            LuaFunction parent = GetParentFunction();
            if (parent == null)
                return; // we in root UwU

            // create Upvalues List from parent
            int functionIndex = parent.Functions.IndexOf(this.Func);
            for (int i = 0; i < parent.Instructions.Count; i++)
            {
                // TODO: bugfix
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
                                 
                                    // TODO: bugfix false locals
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

                        //if (globalName != "")
                        //    this.Name = globalName + ":" + this.Name;

                        break;
                    case LuaOpcode.SETUPVAL:
                        // NOTE: check all 'MOV 0 Bx' after CLOSURE & SETUPVALUE
                        // NOTE: these are only used at runtime to set/get values?
                        var test2 = instr.Bx;
                        break;
                }
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
                    if(this.Blocks[i].Lines[j].Instr.OpCode == LuaOpcode.RETURN)
                    {
                        // check if previous 1/2 is a TAILCALL
                        bool erase = false;
                        if (j >= 1 && this.Blocks[i].Lines[j - 1].Instr.OpCode == LuaOpcode.TAILCALL)
                            erase = true;
                        else if (j >= 2 && this.Blocks[i].Lines[j - 2].Instr.OpCode == LuaOpcode.TAILCALL)
                            erase = true;

                        if (erase)
                        {
                            this.Blocks[i].Lines[j].Op1 = "-- TAILCALL RETURN"; // erase keyword
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

            foreach (var b in this.Blocks)
                if (b.GetConditionLine() != null && b.IfChainIndex > 0)
                    b.Optimize();
        }

        public void Complete()
        {
            GenerateBlocks();
            HandleTailcallReturns(); // fix returns
            OutlineConditions();
#if !DEBUG
            Realign(); // complete?
#endif
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
