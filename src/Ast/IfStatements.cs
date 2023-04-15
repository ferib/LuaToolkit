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

    public class IfElseStatement : Statement
    {
        public IfElseStatement(Expression expression, Statement ifStatement,
            Statement elseStatement)
        {
            Expression = expression;
            IfBody = ifStatement;
            ElseBody = elseStatement;
            Type = STATEMENT_TYPE.IF_ELSE;
        }

        public IfElseStatement(Expression expression)
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

    public class ElseIfStatement : Statement
    {
        public ElseIfStatement(Expression expr, Statement statement)
        {
            ElseIfStatements = new List<IfStatement>
            {
                new IfStatement(expr, statement)
            };
        }

        public ElseIfStatement(IfStatement ifStatement)
        {
            ElseIfStatements = new List<IfStatement>
            {
                ifStatement
            };
        }

        public void AddStatement(Expression expr, Statement statement)
        {
            ElseIfStatements.Add(new IfStatement(expr, statement));
        }

        public void AddStatement(IfStatement ifStatement)
        {
            ElseIfStatements.Add(ifStatement);
        }

        public override string Dump()
        {
            string result = "";
            foreach (var ifStatement in ElseIfStatements)
            {
                result += "if ";
                result += ifStatement.Expression.Dump();
                result += " then" + StringUtil.NewLineChar;
                result += ifStatement.Statement.Dump();
                if (ElseIfStatements.IndexOf(ifStatement) != ElseIfStatements.Count - 1)
                {
                    result += "else ";
                }
            }
            result += "end" + StringUtil.NewLineChar;
            return result;
        }

        public override AstType Execute()
        {
            foreach(var ifStatement in ElseIfStatements)
            {
                if(ifStatement.Expression.Execute().Bool)
                {
                    return ifStatement.Statement.Execute();
                }
            }
            return new AstType();
        }

        List<IfStatement> ElseIfStatements;
    }

    public class ElseIfElseStatement : Statement
    {
        public ElseIfElseStatement(Expression expr, Statement statement)
        {
            ElseIfStatements = new List<IfStatement>
            {
                new IfStatement(expr, statement)
            };
        }

        public ElseIfElseStatement(IfStatement ifStatement)
        {
            ElseIfStatements = new List<IfStatement>
            {
                ifStatement
            };
        }

        public ElseIfElseStatement(Expression expr, Statement statement, Statement elseStatment)
        {
            ElseIfStatements = new List<IfStatement>
            {
                new IfStatement(expr, statement)
            };
            ElseStatement = elseStatment;
        }

        public ElseIfElseStatement(IfStatement ifStatement, Statement elseStatment)
        {
            ElseIfStatements = new List<IfStatement>
            {
                ifStatement
            };
            ElseStatement = elseStatment;
        }

        public void AddStatement(Expression expr, Statement statement)
        {
            ElseIfStatements.Add(new IfStatement(expr, statement));
        }

        public void AddStatement(IfStatement ifStatement)
        {
            ElseIfStatements.Add(ifStatement);
        }

        public override string Dump()
        {
            string result = "";
            foreach (var ifStatement in ElseIfStatements)
            {
                result += "if ";
                result += ifStatement.Expression.Dump();
                result += " then" + StringUtil.NewLineChar;
                result += ifStatement.Statement.Dump() ;
                result += "else";
                if (ElseIfStatements.IndexOf(ifStatement) != ElseIfStatements.Count - 1)
                {
                    result += " ";
                }
            }
            result += StringUtil.NewLineChar;
            result += ElseStatement.Dump();
            result += "end" + StringUtil.NewLineChar;
            return result;
        }

        public override AstType Execute()
        {
            foreach (var ifStatement in ElseIfStatements)
            {
                if (ifStatement.Expression.Execute().Bool)
                {
                    return ifStatement.Statement.Execute();
                }
            }
            return ElseStatement.Execute();
        }

        List<IfStatement> ElseIfStatements;
        Statement ElseStatement;
    }
}
