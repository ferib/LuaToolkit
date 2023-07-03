using LuaToolkit.Ast;
using LuaToolkit.Ast.Passes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Disassembler.Passes
{
    internal class InstructionPassManager
    {
        public InstructionPassManager()
        {
            mPasses = new List<BaseInstructionPass>();
        }

        public bool RunOnFunction(Function func)
        {
            foreach (var pass in mPasses)
            {
                pass.InitPass();
                pass.RunOnFunction(func);
                foreach (Function subFunc in func.Functions)
                {
                    pass.RunOnFunction(subFunc);
                }
                pass.FinalizePass();
            }
            return true;
        }

        public void AddPass(BaseInstructionPass pass)
        {
            mPasses.Add(pass);
        }

        private List<BaseInstructionPass> mPasses;
    }
}
