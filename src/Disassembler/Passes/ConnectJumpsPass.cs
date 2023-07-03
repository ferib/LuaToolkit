using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Disassembler.Passes
{
    internal class ConnectJumpsPass : BaseInstructionPass
    {
        public override bool RunOnFunction(Function function)
        {
            bool result = true;
            foreach(var instr in function.Instructions)
            {
                result &= RunOnInstruction(instr);
            }
            return result;
        }

        public bool RunOnInstruction(Instruction instr)
        {
            if(instr.OpCode == LuaOpcode.JMP)
            {
                var jmpInstrOrErr = InstructionConvertor<JmpInstruction>.Convert(instr);
                if(jmpInstrOrErr.HasError())
                {
                    Debug.Assert(false, jmpInstrOrErr.GetError());
                    return false;
                }
                var jmpInstr = jmpInstrOrErr.Value;
                var jmpResult = jmpInstr.TargetAddress; 
                var target = instr.Function.GetInstructionAtLine(jmpResult);
                jmpInstr.Target = target;
                target.Branchers.Add(jmpInstr);
            }
            return true;
        }
    }
}
