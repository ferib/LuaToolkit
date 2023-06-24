using LuaToolkit.Ast;
using LuaToolkit.Disassembler;
using LuaToolkit.Disassembler.ControlFlowAnalysis;
using System;
using System.Collections.Generic;
using System.IO;

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
            var rootGroup = new InstructionGroup();
            rootGroup.Name = "Root Group";
            groupMaker.Run(RootFunction.Instructions, rootGroup);
            var result = rootGroup.Dump();

            var outpath = AppDomain.CurrentDomain.BaseDirectory;
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(outpath, "GroupDump.txt")))
            {
                outputFile.Write(result);
            }

            var func = ASTParser.Parse(RootFunction, rootGroup);
            RunPasses passes = new RunPasses();
            passes.Run(func);
            return func.Dump();
        }
    }
}
