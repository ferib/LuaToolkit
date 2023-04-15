using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Ast.Passes
{
    public class ParseJmpPass : BaseFunctionPass
    {
        public override bool RunOnFunction(FunctionDefinitionStatement function)
        {
            for(int i = 0; i < function.StatementList.Statements.Count; ++i)
            {
                var block = function.StatementList.Statements[i];
                var res = Convertor<StatementList>.Convert(block);
                if(res.HasError())
                {
                    Debug.Assert(false, "Every block should be a statement list");
                    continue;
                }
                RunOnBlock(res.Value);
            }

            return true;
        }

        public bool RunOnBlock(StatementList block)
        {
            var ifOrErr = GetIfStatement(block);
            if(ifOrErr.HasError())
            {
                return false;
            }
            var ifIndex = block.Statements.IndexOf(ifOrErr.Value);

            // After the ifstatement there is always a jump.
            if(ifIndex >= block.Statements.Count-1)
            {
                return false;
            }
            var jumpStatementOrErr = Convertor<JumpStatement>.Convert(block.Statements[ifIndex+1]);
            if(jumpStatementOrErr.HasError())
            {
                return false;
            }
            
            
            // If the ifbody ends with a jmp, the chain continues.
            var ifBody = Convertor<StatementList>.Convert(ifOrErr.Value.Statement);
            var ifBodyJmp = GetJmpStatement(ifBody.Value);
            if(ifBodyJmp.HasError())
            {
                // If body does not end with a jmp, if chain stops here.
                // The jump in the if is the next block, to skip the if body
                var blockindex = block.Parent.Statements.IndexOf(block);
                block.Parent.Insert(blockindex + 1, jumpStatementOrErr.Value.Statement);
                block.Statements.Remove(jumpStatementOrErr.Value);
                return true;
            }
            // The jmp inside the ifbody is to jump out of it
            var index = block.Parent.Statements.IndexOf(block);
            block.Parent.Insert(index + 1, ifBodyJmp.Value.Statement);
            ifBody.Value.Statements.Remove(ifBodyJmp.Value);
            
            // Also process the else block
            // RunOnBlock(jumpStatement.Value);
            //if the ifbody ends with a jmp `jumpStatement` is the next statement in the if chain
            var ifElse = new IfElseStatement(ifOrErr.Value.Expression, ifOrErr.Value.Statement, jumpStatementOrErr.Value.Statement);
            block.Statements.Remove(jumpStatementOrErr.Value);
            block.Statements[ifIndex] = ifElse;
            return true;
        }

        public Expected<IfStatement> GetIfStatement(StatementList block)
        {
            foreach(var statement in block.Statements)
            {
                var ifOrErr = Convertor<IfStatement>.Convert(statement);
                if(ifOrErr.HasError())
                {
                    continue;
                }
                return ifOrErr;
            }
            return new Expected<IfStatement>("Block does not contain if");
        }

        public Expected<JumpStatement> GetJmpStatement(StatementList block)
        {
            foreach (var statement in block.Statements)
            {
                var ifOrErr = Convertor<JumpStatement>.Convert(statement);
                if (ifOrErr.HasError())
                {
                    continue;
                }
                return ifOrErr;
            }
            return new Expected<JumpStatement>("Block does not contain if");
        }
    }
}
