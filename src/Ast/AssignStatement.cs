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
            this.Var = var;
            Expression = expression;
            Type = STATEMENT_TYPE.ASSIGN;
        }
        public override string Dump()
        {
            string result = "";
            result += Var.Dump() + " = " + Expression.Dump() + StringUtil.NewLineChar;
            return result;
        }

        public override AstType Execute()
        {
            Var.Content = Expression.Execute();
            return TypeCreator.CreateBool(Var.Content.Assigned);
        }

        public Variable Var;

        public Expression Expression;
    }
}
