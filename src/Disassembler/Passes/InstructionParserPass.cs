using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Disassembler.Passes
{
    internal class InstructionParserPass : BaseInstructionPass
    {
        public override bool RunOnFunction(Function function)
        {
            bool result = true;
            for(int i = 0; i < function.Instructions.Count; ++i)
            {
                result &= RunOnInstruction(function.GetInstruction(i));
            }
            return result;
        }

        public bool RunOnInstruction(Instruction instruction)
        {
            var newInstr = CreateInstructionKind(instruction);
            newInstr.Function = instruction.Function; 
            var index = instruction.Function.Instructions.IndexOf(instruction);

            // Replace instruction in function with new instruction
            instruction.Function.Instructions[index] = newInstr;

            return true;
        }

        public Instruction CreateInstructionKind(Instruction instruction)
        {
            switch (instruction.OpCode)
            {
                case LuaOpcode.MOVE:
                    return new MoveInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.LOADK:
                    return new LoadKInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.LOADBOOL:
                    return new LoadBoolInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.LOADNIL:
                    return new LoadNilInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.GETUPVAL:
                    return new GetUpvalInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.GETGLOBAL:
                    return new GetGlobalInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.GETTABLE:
                    return new GetTableInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.SETGLOBAL:
                    return new SetGlobalInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.SETUPVAL:
                    return new SetUpvalInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.SETTABLE:
                    return new SetTableInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.NEWTABLE:
                    return new NewTableInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.SELF:
                    return new SelfInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.ADD:
                    return new AddInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.SUB:
                    return new SubInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.MUL:
                    return new MulInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.DIV:
                    return new DivInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.MOD:
                    return new ModInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.POW:
                    return new PowInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.UNM:
                    return new UnmInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.NOT:
                    return new NotInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.LEN:
                    return new LenInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.CONCAT:
                    return new ConcatInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.JMP:
                    return new JmpInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.EQ:
                    return new EqInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.LT:
                    return new LtInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.LE:
                    return new LeInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.TEST:
                    return new TestInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.TESTSET:
                    return new TestSetInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.CALL:
                    return new CallInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.TAILCALL:
                    return new TailCallInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.RETURN:
                    return new ReturnInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.FORLOOP:
                    return new ForLoopInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.FORPREP:
                    return new ForPrepInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.TFORLOOP:
                    return new TForLoopInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.SETLIST:
                    return new SetListInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.CLOSE:
                    return new CloseInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.CLOSURE:
                    return new ClosureInstruction(instruction.Data, instruction.LineNumber);
                case LuaOpcode.VARARG:
                    return new VarArgInstruction(instruction.Data, instruction.LineNumber);
                default:
                    Debug.Assert(false, "Instruction Opcode not implemente: " + instruction.OpCode.ToString());
                    return instruction;
            }
        }
    }
}
