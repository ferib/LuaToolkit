using LuaToolkit.Ast.Passes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    internal class RunPasses
    {
        public RunPasses()
        {
            mPassManger = new PassManager();
            mPassManger.AddPass(new VarArgPropegation());
            mPassManger.AddPass(new ParseJmpPass());
        }

        public bool Run(FunctionDefinitionStatement func)
        {
           return mPassManger.RunOnFunction(func);
        }

        private PassManager mPassManger;
    }
}
