using LuaToolkit.Ast;
using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Disassembler
{
    static public class InstructionConvertor<T> where T : Instruction
    {
        static public Expected<T> Convert(Instruction instruction)
        {
            if (instruction == null)
            {
                return new Expected<T>("Cannot convert nullptr");
            }
            if (typeof(T) == instruction.GetType())
            {
                return instruction as T;
            }

            return new Expected<T>("Cannot convert " +
                instruction.GetType().ToString() + " to " + typeof(T).ToString());
        }
    }

    static public class InstructionUtil
    {
        public const int BITRK = (1 << (Instruction.SIZE_B - 1));
        static public bool IsCondition(Instruction instruction)
        {
            switch(instruction.OpCode)
            {
                case LuaOpcode.TEST:
                case LuaOpcode.EQ:
                case LuaOpcode.LE:
                case LuaOpcode.LT:
                    return true;
                default:
                    return false;

            }
        }

        static public int RegToConstIndex(int val)
        {
            // val & ~BITRK should be it according to LUA source
            // But does not work for some reason
            return Math.Abs(val - Instruction.MAX_ARG_B);
        }

        static public int RegBxToConstIndex(int val)
        {
            return Math.Abs(val - Instruction.MAX_ARG_Bx);
        }

        static public bool IsConstant(int val)
        {
            return (val & BITRK) > 0;
        }

        static public ByteConstant GetConstant(Instruction instr, int index)
        {
            return instr.Function.Constants[index];
        }

        static public Instruction GetPreviousInstruction(Instruction instr)
        {
            return instr.Function.GetInstructionAtLine(instr.LineNumber - 1);
        }

        static public Instruction GetNextInstruction(Instruction instr)
        {
            return instr.Function.GetInstructionAtLine(instr.LineNumber + 1);
        }

        static public List<Instruction> GetRange(Instruction first, Instruction end, List<Instruction> instructions)
        {
            var firstIndex = instructions.IndexOf(first);
            var lastIndex = instructions.IndexOf(end);
            return instructions.GetRange(firstIndex, lastIndex - firstIndex + 1);
        }
    }
}
