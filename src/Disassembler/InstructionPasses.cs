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
            mPassManager.AddPass(new InstructionDumper());
        }

        public bool Run(Function functions)
        {
            return mPassManager.RunOnFunction(functions);
        }

        private InstructionPassManager mPassManager;
    }
}
