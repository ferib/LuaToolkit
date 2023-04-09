using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Ast
{
    public class IfStatement : Statement
    {
        public IfStatement(Expression expression, Statement statement)
        {
            Expression = expression;
            Statement = statement;
            Type = STATEMENT_TYPE.IF;
        }

        public IfStatement(Expression expression)
        {
            Expression = expression;
            Type = STATEMENT_TYPE.IF;
        }
        public override string Dump()
        {
            // Debug.Assert(Expression != null);
            // Debug.Assert(Statement != null);
            string result = "";
            result += "if ";
            result += Expression.Dump();
            result += " then" + StringUtil.NewLineChar;
            if (Statement != null)
            {
                result += Statement.Dump();
            }
            result += "end" + StringUtil.NewLineChar;
            return result;
        }

        public override AstType Execute()
        {
            Debug.Assert(Expression != null);
            Debug.Assert(Statement != null);
            if (Expression.Execute().Bool)
            {
                return Statement.Execute();
            }
            return new AstType();
        }

        public Expression Expression;
        public Statement Statement;
    }

    public class IfElseStatment : Statement
    {
        public IfElseStatment(Expression expression, Statement ifStatement,
            Statement elseStatement)
        {
            Expression = expression;
            IfBody = ifStatement;
            ElseBody = elseStatement;
            Type = STATEMENT_TYPE.IF_ELSE;
        }

        public IfElseStatment(Expression expression)
        {
            Expression = expression;
            Type = STATEMENT_TYPE.IF_ELSE;
        }
        public override string Dump()
        {
            Debug.Assert(Expression != null, "There should always be an expression");
            Debug.Assert(IfBody != null, "There should always be an if body");
            Debug.Assert(ElseBody != null, "There should always be an else body");
            string result = "";
            result += "if ";
            result += Expression.Dump();
            result += " then" + StringUtil.NewLineChar;
            result += IfBody.Dump();
            result += "else" + StringUtil.NewLineChar;
            result += ElseBody.Dump();
            result += "end" + StringUtil.NewLineChar;
            return result;
        }

        public override AstType Execute()
        {
            if (Expression.Execute().Bool)
            {
                return IfBody.Execute();
            }
            return ElseBody.Execute();
        }

        public Expression Expression;
        public Statement IfBody;
        public Statement ElseBody;
    }
}
