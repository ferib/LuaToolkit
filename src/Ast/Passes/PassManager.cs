using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast.Passes
{
    internal class PassManager
    {
        public PassManager()
        {
            mPasses = new List<BaseFunctionPass>();
        }
        public bool RunOnFunction(FunctionDefinitionStatement func)
        {
            foreach(var pass in mPasses)
            {
                pass.RunOnFunction(func);
            }
            return true;
        }

        public void AddPass(BaseFunctionPass pass)
        {
            mPasses.Add(pass);
        }

        private List<BaseFunctionPass> mPasses;
    }
}
