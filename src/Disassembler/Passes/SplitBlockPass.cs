using LuaToolkit.Ast;
using LuaToolkit.Ast.Passes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LuaToolkit.Disassembler.Passes
{
    internal class SplitBlockPass : BaseInstructionPass
    {
        public override bool RunOnFunction(Function function)
        {
            bool result = true;
            var newBlock = new Block();
            foreach (var instr in function.Instructions)
            {
                if(ShouldSplitBefore(instr) && newBlock.Instructions.Count != 0)
                {
                    function.AddBlock(newBlock);
                    newBlock = new Block();
                }

                newBlock.AddInstruction(instr);
                
                if(ShouldSplitAfter(instr))
                {
                    function.AddBlock(newBlock);
                    newBlock = new Block();
                }
            }
            function.AddBlock(newBlock);
            return result;
        }

        public bool ShouldSplitBefore(Instruction instruction)
        {
            // If any jump jumps to this instruction we should split.
            return instruction.Branchers.Count > 0;
        }

        public bool ShouldSplitAfter(Instruction instruction)
        {
            switch(instruction.OpCode)
            {
                case LuaOpcode.JMP:
                case LuaOpcode.FORPREP:
                case LuaOpcode.FORLOOP:
                    return true;
                default:
                    return false;
            }
        }

    }
}
