using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class IfElseStatment : Statement
    {
        public IfElseStatment(Expression expression, Statement ifStatement,
            Statement elseStatement)
        {
            Expression = expression;
            IfStatement = ifStatement;
            ElseStatement = elseStatement;
        }
        public override string Dump()
        {
            string result = "";
            result += "if ";
            result += Expression.Dump();
            result += "then" + StringUtil.NewLineChar;
            result += IfStatement.Dump();
            result += "else";
            result += ElseStatement.Dump();
            result += "end" + StringUtil.NewLineChar;
            return result;
        }

        public override AstType Execute()
        {
            if (Expression.Execute().Bool)
            {
                return IfStatement.Execute();
            }
            return ElseStatement.Execute();
        }

        public Expression Expression;
        public Statement IfStatement;
        public Statement ElseStatement;
    }
}
