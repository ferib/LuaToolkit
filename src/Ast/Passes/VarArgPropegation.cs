using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Ast.Passes
{
    public class VarArgPropegation : BaseFunctionPass
    {
        public override bool RunOnFunction(FunctionDefinitionStatement function)
        {
            for (int i = 0; i < function.StatementList.Statements.Count; ++i)
            {
                var block = function.StatementList.Statements[i];
                var res = Convertor<StatementList>.Convert(block);
                if (res.HasError())
                {
                    Debug.Assert(false, "Every block should be a statement list");
                    continue;
                }
                // If we found 1 VarArg expression we can return.
                if(RunOnBlock(function, res.Value))
                {
                    return true;
                }
            }

            return false;
        }

        public bool RunOnBlock(FunctionDefinitionStatement function, StatementList block)
        {
            foreach (var stmt in block.Statements)
            {
                var assignStmt = Convertor<AssignStatement>.Convert(stmt);
                if(assignStmt.HasError())
                {
                    continue;
                }
                var expr = assignStmt.Value.Expression;
                // If we find 1 assign statement with VarArg we know the function is var arg.
                if(expr.Type == EXPRESSION_TYPE.VAR_ARG)
                {
                    function.VarArg = true;
                    return true;
                }
            }
            return false;
        }
    }
}
