using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class AssignStatement : Statement
    {
        public AssignStatement(Variable var, Expression expression)
        {
            this.var = var;
            Expression = expression;
        }
        public override string Dump()
        {
            string result = "";
            result += var.Dump() + " = " + Expression.Dump() + StringUtil.NewLineChar;
            return result;
        }

        public override AstType Execute()
        {
            var.Content = Expression.Execute();
            return TypeCreator.CreateBool(var.Content.Assigned);
        }

        public Variable var;

        public Expression Expression;
    }
}
