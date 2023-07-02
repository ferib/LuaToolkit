using LuaToolkit.Ast;
using LuaToolkit.Disassembler;
using LuaToolkit.Disassembler.ControlFlowAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LuaToolkit.Decompiler
{
    public class LuaDecompiler
    {
        private LuaDecoder Decoder;
        private Function RootFunction => this.Decoder.File.Function;

        public LuaDecompiler(LuaDecoder decoder)
        {
            this.Decoder = decoder;
        }

        public string Decompile(bool debugInfo = false)
        {
            RootFunction.Name = "CRoot"; // or maybe main?
            InstructionPasses instrPasses = new InstructionPasses();
            instrPasses.Run(RootFunction);

            var groupMaker = new InstructionGroupMaker();
            var outpath = AppDomain.CurrentDomain.BaseDirectory;

            RunPasses passes = new RunPasses();
            var astParser = new ASTParser();

            var sb = new StringBuilder();

            foreach (var subFunc in RootFunction.Functions)
            {
                astParser.Reset();
                var subRootGroup = new InstructionGroup();
                subRootGroup.Name = "Sub Root Group";
                groupMaker.Run(subFunc.Instructions, subRootGroup);
                var subResult = subRootGroup.Dump();
                
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(outpath, "GroupDump.txt")))
                {
                    outputFile.Write(subResult);
                }

                var subFuncDecomp = astParser.Parse(RootFunction, subRootGroup);
                passes.Run(subFuncDecomp);
                sb.AppendLine(subFuncDecomp.Dump());
            }

            var rootGroup = new InstructionGroup();
            rootGroup.Name = "Root Group";
            groupMaker.Run(RootFunction.Instructions, rootGroup);
            var result = rootGroup.Dump();

            using (StreamWriter outputFile = new StreamWriter(Path.Combine(outpath, "GroupDump.txt")))
            {
                outputFile.Write(result);
            }
            astParser.Reset();
            var func = astParser.Parse(RootFunction, rootGroup);
            passes.Run(func);
            sb.AppendLine(func.Dump());
            return sb.ToString();
        }
    }
}
