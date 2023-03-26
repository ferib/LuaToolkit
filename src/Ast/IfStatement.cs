using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class IfStatement : Statement
    {
        public IfStatement(Expression expression, Statement statement)
        {
            Expression = expression;
            Statement = statement; 
        }
        public override string Dump()
        {
            string result = "";
            result += "if ";
            result += Expression.Dump();
            result += "then" + StringUtil.NewLineChar;
            result += Statement.Dump();
            result += "end" + StringUtil.NewLineChar;
            return result;
        }

        public override AstType Execute()
        {
            if(Expression.Execute().Bool)
            {
                return Statement.Execute();
            }
            return new AstType();
        }

        public Expression Expression;
        public Statement Statement;
    }
}
