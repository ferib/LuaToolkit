using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Disassembler.Passes
{
    internal class ConnectForPass : BaseInstructionPass
    {
        public override bool RunOnFunction(Function function)
        {
            bool result = true;
            foreach (var instr in function.Instructions)
            {
                result &= RunOnInstruction(instr);
            }
            return result;
        }

        public bool RunOnInstruction(Instruction instr)
        {
            if (instr.OpCode == LuaOpcode.FORPREP)
            {
                var forPrepOrErr = InstructionConvertor<ForPrepInstruction>.Convert(instr);
                if (forPrepOrErr.HasError())
                {
                    Debug.Assert(false, forPrepOrErr.GetError());
                    return false;
                }
                var forPrepInstr = forPrepOrErr.Value;
                var forPrepTarget = forPrepInstr.TargetAddress;
                var potentialForLoopInstr = instr.Function.GetInstructionAtLine(forPrepTarget);
                var forLoopInstrOrErr = InstructionConvertor<ForLoopInstruction>.Convert(potentialForLoopInstr);
                if (forLoopInstrOrErr.HasError())
                {
                    Debug.Assert(false, forLoopInstrOrErr.GetError());
                    return false;
                }
                var forLoopInstr = forLoopInstrOrErr.Value;
                forPrepInstr.ForLoop = forLoopInstr;
                forLoopInstr.ForPrep = forPrepInstr;

                // Get the target address of the for loop instruction.
                // Should always be the instruction after the for prep
                var forLoopTargetAddr = forLoopInstr.TargetAddress;
                forLoopInstr.Target = instr.Function.GetInstructionAtLine(forLoopTargetAddr);
                Debug.Assert(forLoopTargetAddr == forPrepInstr.LineNumber + 1,
                    "Target of the forloop should always be the instruction after the prep");
            }
            return true;
        }
    }
}
