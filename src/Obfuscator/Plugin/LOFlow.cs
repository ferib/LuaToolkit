using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Decompiler;
using LuaSharpVM.Models;

namespace LuaSharpVM.Obfuscator.Plugin
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
            // lets assume we only have 1 function
            var target = base.Decoder.File.Function.Functions[3].ScriptFunction;

            var oldc = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkMagenta;

            // find start ifchains
            List<LuaScriptBlock> blacklist = new List<LuaScriptBlock>();

            while (blacklist.Count != target.Blocks.FindAll(x => x.IfChainIndex == 0).Count)
            {
                // find next ifchain block start
                var ifStartBlock = target.Blocks.Find(x => x.IfChainIndex == 0 && !blacklist.Contains(x));
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
                    int lastAddr = ifMergeMembers[ifMergeMembers.Count - 1].JumpsTo; // AND
                    //int lastAddr = ifMergeMembers[i+1].JumpsTo; // OR
                    var newblock = GeneareIf(ifMergeMembers[i], lastAddr, ifMergeMembers[i+1].StartAddress);
                    // add rebased?
                    ifMergeMembers.Insert(i+1, newblock);

                    // change StartAddress (TODO: same for main blocks?)
                    newLinesCount += ifMergeMembers[i].Lines.Count;
                    for (int j = i+1; j < ifMergeMembers.Count; j++)
                    {
                        ifMergeMembers[j].StartAddress += ifMergeMembers[i].Lines.Count;
                        if (ifMergeMembers[j].JumpsTo != -1)
                            ifMergeMembers[j].JumpsTo += ifMergeMembers[i].Lines.Count;
                        if (ifMergeMembers[j].JumpsNext != -1)
                            ifMergeMembers[j].JumpsNext += ifMergeMembers[i].Lines.Count;
                    }
                    break;
                }

                // remove old
                target.Blocks.RemoveRange(index, count);
                target.Blocks.InsertRange(index, ifMergeMembers);

                // TODO: rebase other blocks
                for(int i = index+count+1; i < target.Blocks.Count; i++)
                {
                    target.Blocks[i].StartAddress += newLinesCount;
                    if (target.Blocks[i].JumpsTo != -1)
                        target.Blocks[i].JumpsTo += newLinesCount;
                    if (target.Blocks[i].JumpsNext != -1)
                        target.Blocks[i].JumpsNext += newLinesCount;
                }

                // complete
                blacklist.Add(ifStartBlock);
            }

            target.Complete();

            Console.WriteLine(target.Text);
            Console.ForegroundColor = oldc;
        }

        private LuaScriptBlock GeneareIf(LuaScriptBlock old, int jmp, int els)
        {
            var block = new LuaScriptBlock(old.StartAddress, ref old.Decoder, ref old.Func);

            // TODO: take and/or so we can make 10+ condition chains and have 1 OR fuck them all or have 1 critical AND that does the real logic
            block.AddScriptLine(new LuaScriptLine(new LuaInstruction(LuaOpcode.EQ) // TODO: take a random operand here
            {
                A = 1, // true/false
                B = 1, // op1 // TODO: take some random values here
                C = 1, // op2
            }, ref old.Decoder, ref old.Func));

            block.AddScriptLine(new LuaScriptLine(new LuaInstruction(LuaOpcode.JMP)
            {
                sBx = jmp - (old.StartAddress + old.Lines.Count)
            }, ref old.Decoder, ref old.Func));
            block.JumpsNext = els;
            block.JumpsTo = block.StartAddress + block.Lines.Count + block.Lines[block.Lines.Count - 1].Instr.sBx + 2;
            return block;
        }

        public override string GetName()
        {
            return Name;
        }
    }
}
