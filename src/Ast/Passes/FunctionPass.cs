using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast.Passes
{
    public abstract class BaseFunctionPass
    {
        public abstract bool RunOnFunction(FunctionDefinitionStatement function);
    }
}
