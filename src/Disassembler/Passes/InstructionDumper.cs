using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LuaToolkit.Disassembler.Passes
{
    internal class InstructionDumper : BaseInstructionPass
    {
        override public void InitPass()
        {
            strBuilder = new StringBuilder();
        }

        override public void FinalizePass()
        {
            var outpath = AppDomain.CurrentDomain.BaseDirectory;
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(outpath, "InstructionDump.txt")))
            {
                outputFile.Write(strBuilder.ToString());
            }
        }
        public override bool RunOnFunction(Function function)
        {
            strBuilder.Append(function.Name).Append("()").Append(StringUtil.NewLineChar);

            // Dump instructions
            foreach(var block in function.Blocks)
            {
                strBuilder.Append("\tloc_").Append(block.Instructions.First().LineNumber).
                    AppendLine("");
                foreach (var instr in block.Instructions)
                {
                    strBuilder.Append("\t\t").AppendLine(instr.Dump());
                }
            }
            
            strBuilder.AppendLine("end");

            // Dump Constants
            strBuilder.AppendLine("Constants: ");
            foreach (var constant in function.Constants)
            {
                strBuilder.Append("\t").AppendLine(constant.Dump());
            }

            // Dump Upvals
            strBuilder.AppendLine("Upvals: ");
            foreach (var upval in function.Upvals)
            {
                strBuilder.Append("\t").AppendLine(upval);
            }

            // Dump DebugLines
            // Relation between pc and source code lines.
            strBuilder.AppendLine("Debug Lines: ");
            int pc = 0;
            foreach (var debugLine in function.DebugLines)
            {
                strBuilder.Append("\t").Append(pc).Append(" : ")
                    .AppendLine(debugLine.ToString());
                ++pc;
            }

            // Dump Locals
            strBuilder.AppendLine("Locals: ");
            foreach (var local in function.Locals)
            {
                strBuilder.Append("\t").AppendLine(local.Dump());
            }
            strBuilder.AppendLine("");

            return true;
        }

        private StringBuilder strBuilder;
    }
}
