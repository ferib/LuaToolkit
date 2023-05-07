using LuaToolkit.Disassembler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace LuaToolkit.Decompiler
{
    public class LuaScriptFunction
    {
        public int Depth; // unused?
        private LuaDecoder Decoder;
        private LuaFunction Func;

        public bool IsLocal = false;
        public bool HasVarargs = false;
        private List<int> Args;
        private List<string> NameArgs;
        private List<LuaScriptLine> Lines;

        public List<LuaScriptBlock> Blocks;
        private List<int> UsedLocals;

        public string Name
        {
            get
            {
                return this.Func.Name;
            }
            set
            {
                if (this.Func != null && this.Func.Name != null)
                    this.Func.Name = value;
            }
        }


        public LuaScriptFunction(LuaFunction func, LuaDecoder decoder)
        {
            this.Func = func;
            this.Func.ScriptFunction = this; // reference this for lateron
            this.Decoder = decoder;
            this.Lines = new List<LuaScriptLine>();
            this.Blocks = new List<LuaScriptBlock>();
            this.UsedLocals = new List<int>();
            InitArgs(this.Func.ArgsCount);
            this.UsedLocals.AddRange(this.Args);
            HandleUpvalues(); // get upvalues from parent TODO: Bugfix
        }
        public LuaScriptFunction(string name, int argsCount, LuaFunction func, LuaDecoder decoder)
        {
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
        public List<LuaScriptLine> GetLines()
        {
            return this.Lines;
        }
        public List<int> GetUsedLocals()
        {
            return this.UsedLocals;
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
        public LuaFunction GetParentFunction()
        {
            if (this.Decoder.File.Function == this.Func)
                return null; // we root already

            return FindParentFunction(this.Decoder.File.Function);
        }
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
        public void Finalize(bool overwriteBlocks = false, bool debuginfo = false)
        {
            Cleanlines();
            GenerateBlocks(overwriteBlocks, debuginfo);
            UpdateClosures(); // fixes closure name referncing
            HandleTailcallReturns(debuginfo); // fix returns
            OutlineConditions(); // moves IF code above IF chain
        }
        public string Decompile(bool debugInfo = false, bool recompile = false)
        {
            if (this.Blocks.Count == 0 || recompile)
            {
                this.Finalize(false, debugInfo);
            }

            //string result = this.ToString(); // func prototype
            //result += ;

            return this.Decompilebeautiful();
        }
        public void Cleanlines()
        {
            for (int i = 0; i < this.Lines.Count; i++)
                this.Lines[i].ClearLine();
        }

        // NOTE: this is garbage, might as well get rid of it?
        public string Decompilebeautiful()
        {
            // text based because we did wanky things instead of respecting the list	
            int tabCount = 0;

            //string[] lines = GetText().Replace("\r", "").Replace("\t", "").Split('\n');
            string test = GenerateCode();

            test = test.Replace("\r", "");
            string[] lines = test.Split('\n');

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
        //

        
        // keep for internal debug usage?
        //private string GenerateDebugCode()
        //{
        //    string result = "";
        //    int tabLevel = 0;
        //    for (int b = 0; b < this.Blocks.Count; b++)
        //    {
        //        // print block content
        //        for (int i = 0; i < this.Blocks[b].Lines.Count; i++)
        //        {
        //            if (this.Blocks[b].Lines[i].Instr.OpCode == LuaOpcode.CLOSURE)
        //                result += this.Blocks[b].Lines[i].GetFunctionRef().ScriptFunction.Decompile(); // inline func in parent
        //            result += (this.Blocks[b].StartAddress + i).ToString("0000") 
        //                + $": {new string(' ', tabLevel)}" + this.Blocks[b].Lines[i].GetText().Replace("\t", "");
        //        }
        //        result += new string('-', 50) + $" ({this.Blocks[b].JumpsTo}) \r\n";
        //        if (b == this.Blocks.Count - 1)
        //            result += "\r\n"; // keep it clean?
        //    }
        //    return result;
        //}
        private string GenerateCode()
        {
            // prototype
            string result = "";
            string args = "(";
            for (int i = 0; i < this.NameArgs.Count; i++)
            {
                args += this.NameArgs[i];
                if (i < this.NameArgs.Count - 1 || this.HasVarargs)
                    args += ", ";
            }
            args += (this.HasVarargs ? "...)" : ")");
            result = (this.IsLocal ? "local " : "") + $"function {GetName()}{args}\r\n";

            // body
            for (int b = 0; b < this.Blocks.Count; b++)
            {
                for (int i = 0; i < this.Blocks[b].Lines.Count; i++)
                {
                    if (this.Blocks[b].Lines[i].Instr.OpCode == LuaOpcode.CLOSURE)
                        if (this.Blocks[b].Lines[i].GetFunctionRef() != null)
                            result += (this.Blocks[b].Lines[i].GetFunctionRef().ScriptFunction.Decompilebeautiful()); // inline func in parent
                    result += (this.Blocks[b].Lines[i].GetText().Replace("\t", ""));
                }

            }
            return result;
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
                            if (obj.Contains('\"'))
                                cons = new StringConstant(obj.Substring(1, obj.Length - 2) + "\0");
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
                    this.Name = globalName + ":" + this.Name;

                // set line CLOSURE from parent
                parent.ScriptFunction.Lines[i].SetFunctionRef(this.Func);
            }
        }
        private void HandleTailcallReturns(bool debuginfo = false)
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
            for (int i = 0; i < this.Lines.Count; i++)
            {
                if (this.Lines[i].Instr.OpCode != LuaOpcode.CLOSURE)
                    continue;

                if (this.Func.Functions[this.Lines[i].Instr.Bx].ScriptFunction != null)
                    this.Lines[i].Op3 = this.Func.Functions[this.Lines[i].Instr.Bx].ScriptFunction.Name;

            }
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

        /// <summary>
        /// Split lines of a function in separate blocks.
        /// Each block ends with a branch.
        /// </summary>
        private void GenerateBlocksForLines()
        {
            int index = 0;
            while (index < this.Lines.Count)
            {
                LuaScriptBlock newBlock = new LuaScriptBlock(index, this.Decoder, this.Func);
                newBlock.ScriptFunction = this;
                // Add all instructions to the new block until we encounter the end of a block.
                while (index < this.Lines.Count)
                {
                    if (newBlock.AddScriptLine(this.Lines[index]))
                    {
                        // break out of while when next instruction is new block
                        break; 
                    }
                    index++;
                }
                index++;
                this.Blocks.Add(newBlock);
            }
        }

        private void GenerateBlocksImpl()
        {
            // Removes all blocks.
            // Should only be called when we want to generate blocks for the first time
            // or want to overwrite the already generated blocks.
            this.Blocks.Clear();

            GenerateBlocksForLines();

            // add block jumpsFrom and split
            List<KeyValuePair<int, int>> BlockSplitLines = new List<KeyValuePair<int, int>>();// block, line
            foreach(var block in this.Blocks)
            {
                if (block.JumpsTo == -1)
                    continue;

                // Find all blocks (targets) that have a jump to this block.
                var targets = this.Blocks.FindAll(x => x.HasLineNumber(block.JumpsTo));

                // If there are no blocks that jump to this block, continue.
                if (targets.Count == 0)
                    continue;

                // Find a block that starts 

                var targetpair = new KeyValuePair<int, int>(this.Blocks.FindIndex(x => x.StartAddress == targets[0].StartAddress), block.JumpsTo);

                //// Hacky way to check if previous instr is FORLOOP and have it jump to self-ish?
                //var line = this.Blocks[i].Lines[targetpair.Value - 2]; // -1 to rebase index, -1 to get previous line
                //if (line.Instr.OpCode == LuaOpcode.FORLOOP)
                //    targetpair = new KeyValuePair<int, int>(targetpair.Key, targetpair.Value - 1);
                BlockSplitLines.Add(targetpair);

                


                //foreach (var tb in targets)
                //    BlockSplitLines.Add(new KeyValuePair<int, int>(this.Blocks.FindIndex(x => x.StartAddress == tb.StartAddress), i));
            }

            BlockSplitLines = BlockSplitLines.OrderBy(x => x.Value).ToList(); // important to sort by lineNumber or other blocks wont get done otherwise
                                                                              // cut blocks and make new ones
            for (int i = 0; i < BlockSplitLines.Count; i++)
            {
                int k = BlockSplitLines[i].Key;
                int v = BlockSplitLines[i].Value; // -1; // subtract by 1 to allign with Block Index
                if (this.Blocks[k].StartAddress + this.Blocks[k].Lines.Count < v ||
                    this.Blocks[k].StartAddress > v)
                    continue; // already circumcised this boi

                if (this.Blocks.Find(x => x.StartAddress == v) != null)
                    continue; // been there, done that

                LuaScriptBlock splitBlock = new LuaScriptBlock(v, this.Decoder, this.Func);
                for (int j = v - this.Blocks[k].StartAddress; j < this.Blocks[k].Lines.Count; j++)
                {
                    splitBlock.Lines.Add(this.Blocks[k].Lines[j]); // copy from old to new
                    // UGLY
                    this.Blocks[k].Lines[j].Block = splitBlock;
                }


                // delete old lines
                if (splitBlock.Lines.Count > 0)
                {
                    this.Blocks[k].Lines.RemoveRange(v - this.Blocks[k].StartAddress, splitBlock.Lines.Count);
                }

                this.Blocks.Insert(k + 1, splitBlock); // insert new block after modified one
                                                       // update BlockSplitLines indexing
                for (int j = i + 1; j < BlockSplitLines.Count; j++)
                {
                    BlockSplitLines[j] = new KeyValuePair<int, int>(BlockSplitLines[j].Key + 1, BlockSplitLines[j].Value); // offset remaining blocks
                }
            }

            // fix JumpsTo and JumpsNext ?
            this.Blocks.OrderBy(x => x.StartAddress);
            for (int i = 0; i < this.Blocks.Count; i++)
            {
                var block = this.Blocks[i];
                // last return
                if (i == this.Blocks.Count - 1)
                {
                    // Last block shouldnt jump to anywhere
                    block.JumpsTo = -1;
                    block.JumpsNext = -1;
                    if (block.Lines[block.Lines.Count - 1].Instr.OpCode == LuaOpcode.RETURN)
                        block.Lines[block.Lines.Count - 1].Op1 = ""; //"end -- return"; // make empty, will be 'end' in processing of blocks
                    continue;
                }
                if (block.Lines.Count == 0)
                {
                    continue;
                }
                // Get the last instruction of the block (this is always a branch
                switch (block.Lines[block.Lines.Count - 1].Instr.OpCode)
                {
                    // TODO: check which instructions dont pick the next one
                    case LuaOpcode.TFORLOOP:
                    case LuaOpcode.FORLOOP: // calculate LOOP jump
                        short off = 1; // TODO?
                        if ((short)this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Instr.sBx < 0)
                            off = 0; // TODO: VERIFY
                        block.JumpsTo = (block.StartAddress + block.Lines.Count -1) + (short)block.Lines[block.Lines.Count - 1].Instr.sBx + off; // TODO: verify math
                        block.JumpsNext = this.Blocks[i + 1].StartAddress;
                        break; // jmp?
                                //case LuaOpcode.LOADBOOL: // pc++
                                //    this.Blocks[i].JumpsTo = (this.Blocks[i].StartAddress + this.Blocks[i].Lines.Count - 1) + 2; // skips one if C
                                //    this.Blocks[i].JumpsNext = (this.Blocks[i].StartAddress + this.Blocks[i].Lines.Count - 1) + 1; // next block
                                //    break;
                    case LuaOpcode.JMP: // pc++

                        // check previous instruction is a compare instruction 
                        if (block.Lines.Count > 1 && block.Lines[block.Lines.Count - 2].IsCondition()) // check for IF
                        {
                            // if/test/testset
                            short off2 = 1; // TODO?
                            if ((short)this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Instr.sBx < 0)
                                off2 = 0;
                            block.JumpsTo = (block.StartAddress + block.Lines.Count - 1) + (short)block.Lines[block.Lines.Count - 1].Instr.sBx + off2; // TODO: verify math
                            block.JumpsNext = this.Blocks[i + 1].StartAddress;
                        }
                        else
                        {
                            // unknown jump
                            short off2 = 1; // TODO?
                            if ((short)this.Blocks[i].Lines[this.Blocks[i].Lines.Count - 1].Instr.sBx < 0)
                                off2 = 0;
                            block.JumpsTo = (block.StartAddress + block.Lines.Count - 1) + (short)block.Lines[block.Lines.Count - 1].Instr.sBx + off2; // TODO: verify math
                            block.JumpsNext = -1; // this.Blocks[i + 1].StartAddress;
                        }
                        break;
                    default:
                        block.JumpsTo = -1; // erase from possible previous block?
                        block.JumpsNext = this.Blocks[i + 1].StartAddress;
                        break;
                }
                
            }
        }

        private void SetBlocksForJumps()
        {
            foreach(var block in Blocks)
            {
                if(block.JumpsNext != -1)
                {
                    var targets = this.Blocks.FindAll(x => (block.JumpsNext == x.StartAddress));
                    if(targets.Count > 0)
                    {
                        Debug.Assert(targets.Count == 1, "Every block can only jump to 1 block");
                        block.JumpsNextBlock = targets[0];
                        targets[0].BrancherBlocks.Add(block);
                    }
                }
                if(block.JumpsTo != -1)
                {
                    var targets = this.Blocks.FindAll(x => (block.JumpsTo == x.StartAddress));
                    if (targets.Count > 0)
                    {
                        Debug.Assert(targets.Count == 1, "Every block can only jump to 1 block");
                        block.JumpsToBlock = targets[0];
                        foreach(var l in block.Lines)
                        {
                            if(l.IsBranch())
                            {
                                l.JumpsTo = block.JumpsToBlock;
                            }
                        }
                        targets[0].BrancherBlocks.Add(block);
                    }
                }
                
            }
        }

        /// <summary>
        /// Handles chain of ifs:
        /// Example: if A and B then: 
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="block"></param>
        private void HandleIfBlockMergeChain(int blockIndex, LuaScriptBlock block, bool debugInfo)
        {
            try
            {
                // Empty If Statement
                if (block.JumpsTo == block.JumpsNext)
                {
                    // NOTE: Bugfix to prevent merging with next blocks?
#if DEBUG
                    block.Lines[block.Lines.Count - 1].Postfix = "\r\nend -- self\r\n";
#else
                            block.Lines[block.Lines.Count - 1].Postfix = "\r\nend";
#endif
                    block.IfChainIndex = 0; // mark as solved
                    return;
                }

                // merge
                int lastifIndex = -1;
                int bIndex = blockIndex + 1; // search end of IF
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
                if (block.IfChainIndex != -1)
                {
                    // skip if already discovered
                    return; 
                }
                while (ifIndex >= blockIndex)
                {
                    // NOTE: not always the case??
                    // TODO: INF loop somtimes!!
                    var ifbodyBlockEnd = this.Blocks[lastifIndex].JumpsToBlock;
                    //var ifbodyBlockEnd = this.Blocks.ToList().Single(x => x.StartAddress == this.Blocks[lastifIndex].JumpsTo); // end JMP
                    var ifbodyBlockStart = this.Blocks[lastifIndex + 1]; // start +1

                    // NOTE: iterate from end to here to which block it JMPs to
                    bool found = false;
                    int cIndex = this.Blocks.IndexOf(ifbodyBlockEnd); // start from endblock
                    while (cIndex > ifIndex)
                    {
                        // scan if's (and ONLT if's)
                        var conditionLine = this.Blocks[ifIndex].GetConditionLine();
                        
                        if(this.Blocks[ifIndex].JumpsTo != this.Blocks[cIndex].StartAddress || conditionLine == null
                            || !conditionLine.IsCondition())
                        {
                            cIndex--;
                            continue;
                        }
                        
                        found = true;
                        bool jmpsToStart = this.Blocks[ifIndex].JumpsTo == ifbodyBlockStart.StartAddress; // is or?
                        if (jmpsToStart)
                        {
                            if (ifIndex != lastifIndex)
                                conditionLine.Op3 = "or";
                            if (conditionLine.Instr.A == 0)
                                conditionLine.Op2 = conditionLine.Op2.Replace("==", "~=");
                        }
                        else
                        {
                            if (ifIndex != lastifIndex)
                                conditionLine.Op3 = "and";
                            if (conditionLine.Instr.A == 1)
                                conditionLine.Op2 = conditionLine.Op2.Replace("==", "~=");
                        }

                        if (ifIndex != lastifIndex && this.Blocks[ifIndex + 1].GetConditionLine() != null)
                        {
                            this.Blocks[ifIndex + 1].GetConditionLine().Op1 = "";
                        }
                        if (this.Blocks[ifIndex].IfChainIndex == -1)
                        {
                            this.Blocks[ifIndex].IfChainIndex = ifIndex - blockIndex; // NOTE: numbers are NOT correct after rebase!
                        }
                        break;
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
                        if (cIndex < ifIndex || this.Blocks[ifIndex].GetConditionLine() == null) // skip LOOPs?
                            ifIndex--; // subtract or inf loop?? TODO: bugfix, may 
                    }
                } 
            }
            catch (Exception e)
            {
                // TODO: wtf?
                Console.WriteLine(e);
            }
        }

        private void HandleElseBlock(LuaScriptLine branchLine, bool debugInfo)
        {
            if (debugInfo)
            {
                branchLine.Prefix += "else -- ELSE";
            }
            //this.Blocks[i].GetBranchLine().Postfix += "else -- ELSE";
            else
            {
                branchLine.Prefix += "else";
            }
            //this.Blocks[i].GetBranchLine().Postfix += "else";
        }

        /// <summary>
        /// I have no clue what this does, but I think it is to handle the last block of the if.
        /// </summary>
        private void HandleLastBlockOfIf(int currentBlockIndex, LuaScriptBlock currentBlock) 
        {
            // TODO: Validate if this is an actual end?
            var nextBlock = this.Blocks[currentBlockIndex + 1];
            var nextLine = nextBlock.Lines[0];
            var lastLine = currentBlock.Lines[currentBlock.Lines.Count - 1];

            // NOTE: untested?
            var xrefs = this.Blocks.FindAll(x => (x.JumpsTo == currentBlock.StartAddress || x.JumpsNext == currentBlock.StartAddress));
            //var xrefs = this.Blocks.FindAll(x => (x.JumpsTo == block.StartAddress)); // || x.JumpsNext == block.StartAddress));
            //bool okdo = false;
            //if(okdo)
            foreach (var branchee in currentBlock.BrancherBlocks)
            {
                // TODO: take last if??
                if (branchee.IfChainIndex > 0)
                {
                    continue;// only need END for first IF in chain
                }

                //// check if it has an ELSE (ELSE: JMP != -1 && ELSE == -1)
                //if (xrefs[j].JumpsTo == -1) // block.JumpsNext)
                //{
                //    // This is an ELSE
                //    continue;
                //}
                if (!(branchee.JumpsTo == currentBlock.StartAddress || (branchee.JumpsTo != -1 && branchee.JumpsNext == currentBlock.StartAddress)))
                    continue;

                if (lastLine.Instr.OpCode == LuaOpcode.RETURN)
                {
#if DEBUG
                    var index = currentBlock.BrancherBlocks.IndexOf(branchee);
                    lastLine.Postfix += $"\r\nend -- _{index}\r\n"; // ???
#else
                    lastLine.Postfix += $"\r\nend"; // ???
#endif
                }
                else
                {
#if DEBUG
                    var index = currentBlock.BrancherBlocks.IndexOf(branchee);
                    lastLine.Prefix += $"end -- _{index}\r\n";
#else
                    lastLine.Prefix += $"end\r\n";
#endif
                }
            }  
        }
        
        // NOTE: Please do NOT touch this unless you 110% know what you are doing!!!
        private void GenerateBlocks(bool overwriteBlocks = false, bool debuginfo = false)
        {
            // Generate block (if needed)
            if (overwriteBlocks || this.Blocks.Count == 0)
            {
                GenerateBlocksImpl();
                SetBlocksForJumps();
            }

            // Split block on xref/jump back
            for (int currentBlockIndex = 0; currentBlockIndex < this.Blocks.Count; currentBlockIndex++)
            {
                // IF: JMP != -1 && ELSE != -1 (&& GetConditionLine != NULL; ELSE; FORLOOP END (dont care))
                // ELSE: JMP != -1 && ELSE == -1
                // END-BLOCK: JMP == -1 && ELSE != -1
                //   -> END if/func for all blocks JMP == END-BLOCK.Address
                // END: JMP == -1 && ELSE == -1
                var currentBlock = this.Blocks[currentBlockIndex];
                var branchLine = currentBlock.GetBranchLine();
                if (branchLine != null
                    && (branchLine.Instr.OpCode == LuaOpcode.FORLOOP
                      || branchLine.Instr.OpCode == LuaOpcode.TFORLOOP))
                {
                    //if (bline.Instr.OpCode == LuaOpcode.JMP)
                    //{
                    //    if (block.Jump)
                    //    bline.Postfix += "end\r\n";
                    //}
                    //else
                    // NOTE: can have multiple endings?

                    //var xrefs = this.Blocks.FindAll(x => x.GetBranchLine() != null && (x.JumpsTo == block.StartAddress)); // || x.JumpsNext == block.StartAddress));
                    //var xrefs = this.Blocks.FindAll(x => x.GetBranchLine() != null && (x.JumpsTo == block.StartAddress || x.JumpsNext == block.StartAddress));
                    // All blocks that jump to this block.
                    foreach(var brancheeBlock in currentBlock.BrancherBlocks)
                    {
                        if (brancheeBlock.IfChainIndex > 0)
                        {
                            continue;
                        }

                        //// check if it has an ELSE (ELSE: JMP != -1 && ELSE == -1)
                        //if (xrefs[j].JumpsNext == -1)
                        //{
                        //    // This is an ELSE
                        //    continue;
                        //}

                        if (!(brancheeBlock.JumpsTo == currentBlock.StartAddress || (brancheeBlock.JumpsTo != -1 && brancheeBlock.JumpsNext == currentBlock.StartAddress)))
                            continue;

                        //var xbline = xrefs[j].GetBranchLine();
                        //if (xbline == null || (xbline.Instr.OpCode == LuaOpcode.FORLOOP))
                        //    continue;
#if DEBUG
                        var bracheeIndex = currentBlock.BrancherBlocks.IndexOf(brancheeBlock);
                        branchLine.Postfix += $"end -- -{bracheeIndex}\r\n";
#else
                        branchLine.Postfix += $"end\r\n";
#endif
                    }
                }
                else
                {
                    if (currentBlock.JumpsTo != -1 && currentBlock.JumpsNext != -1
                        && currentBlock.GetConditionLine() != null) // IF detected
                    {
                        HandleIfBlockMergeChain(currentBlockIndex, currentBlock, debuginfo);
                    }
                    else if (currentBlock.JumpsTo != -1 && currentBlock.JumpsNext == -1)
                    {
                        // TODO: verify else?
                        HandleElseBlock(branchLine, debuginfo);

                    }
                    else if (currentBlock.JumpsTo == -1 && currentBlock.JumpsNext != -1
                        //&& (bline != null) // && bline.IsBranch())
                        //&& bline.Instr.OpCode != LuaOpcode.FORPREP
                        ) // also make sure if condifition is set (no forloop)
                    {
                        // TODO: Validate if this is an actual end?
                        HandleLastBlockOfIf(currentBlockIndex, currentBlock);

                    }
                    // No clue what this case means.
                    else if (currentBlock.JumpsTo == -1 && currentBlock.JumpsNext == -1)
                    {
                        // IF: 
                        // ELSE: ELSE == -1
                        // END-BLOCK: JMP == -1
                        //   -> END if/func for all blocks JMP == END-BLOCK.Address
                        // END: JMP == -1 && ELSE == -1

                        var lastLine = currentBlock.Lines[currentBlock.Lines.Count - 1];
                        //var xrefs = this.Blocks.FindAll(x => (x.JumpsTo == block.StartAddress));
                        var xrefs = this.Blocks.FindAll(x => (x.JumpsTo == currentBlock.StartAddress || x.JumpsNext == currentBlock.StartAddress));
                        bool doLast = false;
                        if (doLast)
                            for (int j = 0; j < xrefs.Count; j++)
                            {
                                // TODO: take last if??
                                if (xrefs[j].IfChainIndex > 0)
                                    continue; // only need END for first IF in chain

                                // xrefs contains list of ANY blocks landing at location of block,
                                // to make sure it is an if we check for IF or ELSE blocks, block must
                                // have either `JumpsTo` set to us, or `JumpsNext` to us + JumpsTo != -1

                                //if (xrefs[j].JumpsTo != block.StartAddress)
                                //if (xrefs[j].JumpsTo == -1)
                                if (!(xrefs[j].JumpsTo == currentBlock.StartAddress || (xrefs[j].JumpsTo != -1 && xrefs[j].JumpsNext == currentBlock.StartAddress)))
                                //if(xrefs[j].JumpsTo == block.StartAddress)
                                //if (xrefs[j].JumpsTo == block.StartAddress || xrefs[j].JumpsTo == -1)
                                //if (!(xrefs[j].JumpsTo == -1 && xrefs[j].JumpsNext == block.StartAddress))
                                //if (!(xrefs[j].JumpsTo == block.StartAddress && xrefs[j].JumpsNext != -1))
                                //if (!(xrefs[j].JumpsNext == -1 && xrefs[j].GetBranchLine() != null)) // MUST have a branchLine, otherwise its just a follow block
                                //if (
                                //    (xrefs[j].JumpsNext == -1 || xrefs[j].GetBranchLine() == null) // ELSE or not a branch at all
                                //    //|| (xrefs[j].JumpsNext != -1 && xrefs[j].GetBranchLine() == null)
                                //    )
                                {
                                    // This is an ELSE
                                    continue;
                                }

                                if (lastLine.Instr.OpCode == LuaOpcode.RETURN)
#if DEBUG
                                    lastLine.Postfix += $"\r\nend -- {j}\r\n"; // ???
#else
                                lastLine.Postfix += $"\r\nend"; // ???
#endif
                                else
#if DEBUG
                                    lastLine.Prefix += $"end -- {j}\r\n";
#else
                                lastLine.Prefix += $"end\r\n";
#endif
                            }
                        // Do it anyways?
#if DEBUG
                        lastLine.Prefix += $"end -- x\r\n";
#else
                    lastLine.Prefix += $"end\r\n";
#endif

                    }
                }
            }
        }
        private string GetName()
        {
            //if (this.Func.Name == "" || this.Func.Name.Contains("@")) // unknownX
            //{
            //    // TODO: prefix functions so we can distiguins one parent from another? (like: unknown_0_1)
            //    var parent = GetParentFunction();
            //    if (parent == null)
            //        return "unkErr";

            //    // TODO: get all parents?
            //    int unkCount = -1;
            //    for (int i = 0; i < parent.Functions.IndexOf(this.Func); i++)
            //    {
            //        if (parent.Functions[i].ScriptFunction.IsLocal)
            //            unkCount++;
            //    }
            //    return "unknown" + (unkCount + 1); // should give right index?
            //}
            //return this.Name;
            return this.Func.Name;
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
    }

}
