using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Disassembler
{
    public class Function
    {
        public Function()
        {
            Name = "";
            Instructions = new List<Instruction>();
            Constants = new List<ByteConstant>();
            Upvals = new List<string>();
            Functions = new List<Function>();
            DebugLines = new List<int>();
            Locals = new List<Local>();
            Blocks = new List<Block>();
        }
        public string Name { get; set; }
        public int FirstLineNr;
        public int LastLineNr;
        public byte UpvaluesCount;
        public byte ArgsCount;
        public VarArg VarArg;
        public byte MaxStackSize;

        public void AddInstruction(Instruction instruction)
        {
            Debug.Assert(instruction.LineNumber == 
                Instructions.Count + 1, "Instruction Linenumber is incorrect");
            instruction.Function = this;
            Instructions.Add(instruction);
        }

        public Instruction GetInstruction(int index)
        {
            return Instructions[index];
        }

        public Instruction GetInstructionAtLine(int lineNumber)
        {
            var instr = Instructions[lineNumber - 1];
            Debug.Assert(instr.LineNumber == lineNumber, "instruction is at wrong LineNumber");
            return instr;
        }

        public void AddConstant(ByteConstant constant)
        {
            Constants.Add(constant);
        }

        public ByteConstant GetConstant(int index)
        {
            return Constants[index];
        }

        public void AddUpval(string upval)
        {
            Upvals.Add(upval);
        }

        public string GetUpval(int index)
        {
            return Upvals[index];
        }

        public void AddFunction(Function function)
        {
            Functions.Add(function);
        }

        public Function GetFunction(int index)
        {
            return Functions[index];
        }

        public void AddDebugLine(int debLine)
        {
            DebugLines.Add(debLine);
        }

        public int GetDebugLine(int index)
        {
            return DebugLines[index];
        }

        public void AddLocal(Local local)
        {
            Locals.Add(local);
        }

        public Local GetLocal(int index)
        {
            return Locals[index];
        }

        // Lua Bin Structure
        public List<Instruction> Instructions;
        public List<ByteConstant> Constants;
        public List<string> Upvals;
        public List<Function> Functions;
        public List<int> DebugLines;
        public List<Local> Locals;

        // Helpers
        public void AddBlock(Block block)
        {
            block.Parent = this;
            Blocks.Add(block);
        }

        public List<Block> Blocks;
    }

    public class Block
    {
        public Block()
        {
            Instructions = new List<Instruction>();
            Branchers = new List<JmpInstruction>();
        }
        public void AddInstruction(Instruction instr)
        {
            instr.Parent = this;
            Instructions.Add(instr);
            Branchers.AddRange(instr.Branchers);
        }

        public Function Parent
        {
            get;
            set;
        }

        public List<Instruction> Instructions;
        public List<JmpInstruction> Branchers;
    }
}
