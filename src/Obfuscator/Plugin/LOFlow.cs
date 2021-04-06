using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;
using LuaToolkit.Decompiler;
using LuaToolkit.Models;

namespace LuaToolkit.Obfuscator.Plugin
{
    public class LOFlow : LOPlugin
    {
        // tamper with the control flow

        static string desc = "Tempers the control flow to make it harder to understand.";
        private static string Name = "FlowControl";

        public LOFlow(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }

        public override void Obfuscate()
        {
            // NOTE: My job is to add aditional IF checks to each existing IF chain,
            // I will change the IfChainIndex to -1 after im done manipulating a merge
            // so that the LuaScriptFunction can re-discover them, My block instructions
            // will no longer be correct, instead, we will only rely on JumpTo, JumpNext, etc
            // lets assume we only have 1 function
            var target = base.Decoder.File.Function.Functions[3].ScriptFunction;

            var oldc = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkMagenta;

            // find start ifchains
            List<LuaScriptBlock> blacklist = new List<LuaScriptBlock>();

            //while (target.Blocks.FindAll(x => x.IfChainIndex == 0).Count != blacklist.Count)
            while (target.Blocks.FindAll(x => x.IfChainIndex == 0).Count != 0)
            {
                // find next ifchain block start
                var ifStartBlock = target.Blocks.Find(x => x.IfChainIndex == 0 && !blacklist.Contains(x)); // TODO: obsolete: !blacklist.Contains(x)
                // do some magic
                List<LuaScriptBlock> ifMergeMembers = new List<LuaScriptBlock>();

                int index = target.Blocks.IndexOf(ifStartBlock);
                int count = 0;
                do
                {
                    var ifb = target.Blocks[index + count];
                    ifMergeMembers.Add(ifb);
                    count++;
                } while (target.Blocks[index + count].IfChainIndex == count);

                // got all members, do magic
                int newLinesCount = 0;
                for (int i = 0; i < ifMergeMembers.Count-1; i++)
                {
                    int jmpAND = ifMergeMembers[ifMergeMembers.Count - 1].JumpsTo; // AND
                    //int jmpOR = ifMergeMembers[i+1].JumpsTo; // OR
                    var newblock = GeneareIf(ifMergeMembers[i], jmpAND);
                    // add rebased?
                    ifMergeMembers.Insert(i+1, newblock);

                    // change StartAddress (TODO: same for main blocks?)
                    newLinesCount += newblock.Lines.Count;
                    for (int j = i; j < ifMergeMembers.Count; j++)
                    {
                        ifMergeMembers[j].IfChainIndex = -1; // get ready to re-discover
                        //ifMergeMembers[j].IfChainIndex = j-i ;// -1; // get ready to re-discover
                        if(j > i+1)
                        {
                            if (ifMergeMembers[j].JumpsNext != -1)
                                ifMergeMembers[j].JumpsNext += newblock.Lines.Count;
                            ifMergeMembers[j].StartAddress += newblock.Lines.Count;
                        }
                    }
                    break;
                }

                // remove old
                target.Blocks.RemoveRange(index, count);
                target.Blocks.InsertRange(index, ifMergeMembers);

                // TODO: rebase other blocks
                //for (int i = index + count + 1; i < target.Blocks.Count; i++)
                for (int i = index; i < target.Blocks.Count; i++)
                {
                    // rebase jumps if needed
                    if (target.Blocks[i].JumpsTo != -1 && target.Blocks[i].JumpsTo >= target.Blocks[index +count].StartAddress)
                        target.Blocks[i].JumpsTo += newLinesCount;

                    if (i > index + count)
                    {
                        // upper part (needs more rebase)
                        if (target.Blocks[i].JumpsNext != -1)
                            target.Blocks[i].JumpsNext += newLinesCount;
                        target.Blocks[i].StartAddress += newLinesCount;    
                    }
                }

                // complete
                blacklist.Add(ifStartBlock);
            }

            target.Complete(false);
            Console.WriteLine(target.GetText());
            Console.ForegroundColor = oldc;
        }

        private LuaScriptBlock GeneareIf(LuaScriptBlock old, int jmp, bool els = true)
        {
            var block = new LuaScriptBlock(old.StartAddress + old.Lines.Count, old.GetDecoder(), old.GetFunc());
            // TODO: take and/or so we can make 10+ condition chains and have 1 OR fuck them all or have 1 critical AND that does the real logic
            block.AddScriptLine(new LuaScriptLine(new LuaInstruction(LuaOpcode.EQ) // TODO: take a random operand here
            {
                A = 0, // true/false
                B = 1, // op1 // TODO: take some random values here
                C = 1, // op2
            }, old.GetDecoder(), old.GetFunc()));

            block.AddScriptLine(new LuaScriptLine(new LuaInstruction(LuaOpcode.JMP)
            {
                sBx = jmp - (old.StartAddress + old.Lines.Count)
            }, old.GetDecoder(), old.GetFunc()));

            block.JumpsNext = block.StartAddress + block.Lines.Count; 
            if (!els) 
                block.JumpsNext = -1;
            block.JumpsTo = jmp;

            return block;
        }

        public override string GetName()
        {
            return Name;
        }
    }
}
