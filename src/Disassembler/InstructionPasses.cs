using LuaToolkit.Disassembler.Passes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Disassembler
{
    public class InstructionPasses
    {
        public InstructionPasses()
        {
            mPassManager = new InstructionPassManager();
            mPassManager.AddPass(new InstructionParserPass());
            mPassManager.AddPass(new ConnectJumpsPass());
            mPassManager.AddPass(new ConnectForPass());
            mPassManager.AddPass(new SplitBlockPass());
            // Uncomment to write out debug data
            // mPassManager.AddPass(new InstructionDumper());
        }

        public bool Run(Function function)
        {
            return mPassManager.RunOnFunction(function);
        }

        private InstructionPassManager mPassManager;
    }
}
