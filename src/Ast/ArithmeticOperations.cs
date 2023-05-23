using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class AddExpression : Expression
    {
        public AddExpression(Expression lhs, Expression rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Type = EXPRESSION_TYPE.ADD;
        }
        public override string Dump()
        {
            string result = "";
            result += Lhs.Dump();
            result += " + ";
            result += Rhs.Dump();
            return result;
        }

        public override AstType Execute()
        {
            return Lhs.Execute() + Rhs.Execute();
        }

        Expression Lhs;
        Expression Rhs;
    }

    public class SubExpression : Expression
    {
        public SubExpression(Expression lhs, Expression rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Type = EXPRESSION_TYPE.SUB;
        }
        public override string Dump()
        {
            string result = "";
            result += Lhs.Dump();
            result += " - ";
            result += Rhs.Dump();
            return result;
        }

        public override AstType Execute()
        {
            return Lhs.Execute() - Rhs.Execute();
        }

        Expression Lhs;
        Expression Rhs;
    }

    public class MulExpression : Expression
    {
        public MulExpression(Expression lhs, Expression rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Type = EXPRESSION_TYPE.MUL;
        }
        public override string Dump()
        {
            string result = "";
            result += Lhs.Dump();
            result += " * ";
            result += Rhs.Dump();
            return result;
        }

        public override AstType Execute()
        {
            return Lhs.Execute() * Rhs.Execute();
        }

        Expression Lhs;
        Expression Rhs;
    }

    public class DivExpression : Expression
    {
        public DivExpression(Expression lhs, Expression rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Type = EXPRESSION_TYPE.DIV;
        }
        public override string Dump()
        {
            string result = "";
            result += Lhs.Dump();
            result += " / ";
            result += Rhs.Dump();
            return result;
        }

        public override AstType Execute()
        {
            return Lhs.Execute() / Rhs.Execute();
        }

        Expression Lhs;
        Expression Rhs;
    }

    public class ModExpression : Expression
    {
        public ModExpression(Expression lhs, Expression rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Type = EXPRESSION_TYPE.DIV;
        }
        public override string Dump()
        {
            string result = "";
            result += Lhs.Dump();
            result += " / ";
            result += Rhs.Dump();
            return result;
        }

        public override AstType Execute()
        {
            return Lhs.Execute() % Rhs.Execute();
        }

        Expression Lhs;
        Expression Rhs;
    }

    public class PowExpression : Expression
    {
        public PowExpression(Expression lhs, Expression rhs)
        {
            Lhs = lhs;
            Rhs = rhs;
            Type = EXPRESSION_TYPE.POW;
        }
        public override string Dump()
        {
            string result = "";
            result += Lhs.Dump();
            result += " ^ ";
            result += Rhs.Dump();
            return result;
        }

        public override AstType Execute()
        {
            return AstType.Power(Lhs.Execute(), Rhs.Execute());
        }

        Expression Lhs;
        Expression Rhs;
    }

    public class NegationExpression : Expression
    {
        public NegationExpression(Expression expr)
        {
            Expr = expr;
            Type = EXPRESSION_TYPE.NEG;
        }
        public override string Dump()
        {
            string result = "-";
            result += Expr.Dump();
            return result;
        }

        public override AstType Execute()
        {
            return -Expr.Execute();
        }

        Expression Expr;
    }
}
