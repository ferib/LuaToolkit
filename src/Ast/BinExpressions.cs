using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class AndExpression : Expression
    {
        public AndExpression(Expression expr1, Expression expr2)
        {
            Expr1 = expr1;
            Expr2 = expr2;
            Type = EXPRESSION_TYPE.AND;
        }
        public override string Dump()
        {
            string result = "";
            result += Expr1.Dump();
            result += " and ";
            result += Expr2.Dump();
            return result;
        }

        public override AstType Execute()
        {
            var result = Expr1.Execute();
            if(result.Bool)
            {
                return Expr2.Execute();
            }
            return result;
        }

        Expression Expr1;
        Expression Expr2;
    }

    public class OrExpression : Expression
    {
        public OrExpression(Expression expr1, Expression expr2)
        {
            Expr1 = expr1;
            Expr2 = expr2;
            Type = EXPRESSION_TYPE.OR;
        }
        public override string Dump()
        {
            string result = "";
            result += Expr1.Dump();
            result += " or ";
            result += Expr2.Dump();
            return result;
        }

        public override AstType Execute()
        {
            var result = Expr1.Execute();
            if (result.Bool)
            {
                return result;
            }
            return Expr2.Execute(); ;
        }

        Expression Expr1;
        Expression Expr2;
    }

    public class NotExpression : Expression
    {
        public NotExpression(Expression expr)
        {
            Expr = expr;
            Type = EXPRESSION_TYPE.NOT;
        }
        public override string Dump()
        {
            string result = "not ";
            result += Expr.Dump();
            return result;
        }

        public override AstType Execute()
        {
            var result = Expr.Execute();
            result.Bool = !result.Bool;
            return result;
        }

        Expression Expr;
    }

    public class EqualExpression : Expression
    {
        public EqualExpression(Expression expr1, Expression expr2)
        {
            Expr1 = expr1;
            Expr2 = expr2;
            Type = EXPRESSION_TYPE.EQ;
        }
        public override string Dump()
        {

            string result = "";
            result += Expr1.Dump();
            result += " == ";
            result += Expr2.Dump();
            return result;
        }

        public override AstType Execute()
        {
            var result1 = Expr1.Execute();
            var result2 = Expr2.Execute();
            return result1.Equals(result2);
        }

        Expression Expr1;
        Expression Expr2;
    }

    public class InequalsExpression : Expression
    {
        public InequalsExpression(Expression expr1, Expression expr2)
        {
            Expr1 = expr1;
            Expr2 = expr2;
            Type = EXPRESSION_TYPE.NOT_EQ;
        }
        public override string Dump()
        {

            string result = "";
            result += Expr1.Dump();
            result += " != ";
            result += Expr2.Dump();
            return result;
        }

        public override AstType Execute()
        {
            var result1 = Expr1.Execute();
            var result2 = Expr2.Execute();
            var result3 = result1.Equals(result2);
            result3.Bool = !result3.Bool;
            return result3;
        }

        Expression Expr1;
        Expression Expr2;
    }

    public class LessThanExpression : Expression
    {
        public LessThanExpression(Expression expr1, Expression expr2)
        {
            Expr1 = expr1;
            Expr2 = expr2;
            Type = EXPRESSION_TYPE.LESS_THAN;
        }
        public override string Dump()
        {

            string result = "";
            result += Expr1.Dump();
            result += " < ";
            result += Expr2.Dump();
            return result;
        }

        public override AstType Execute()
        {
            var result1 = Expr1.Execute();
            var result2 = Expr2.Execute();
            return result1.SmallerThan(result2);
        }

        Expression Expr1;
        Expression Expr2;
    }

    public class BiggerThanExpression : Expression
    {
        public BiggerThanExpression(Expression expr1, Expression expr2)
        {
            Expr1 = expr1;
            Expr2 = expr2;
            Type = EXPRESSION_TYPE.BIGGER_THAN;
        }
        public override string Dump()
        {

            string result = "";
            result += Expr1.Dump();
            result += " > ";
            result += Expr2.Dump();
            return result;
        }

        public override AstType Execute()
        {
            var result1 = Expr1.Execute();
            var result2 = Expr2.Execute();
            return result1.LargerThan(result2);
        }

        Expression Expr1;
        Expression Expr2;
    }

    public class LessOrEqualThanExpression : Expression
    {
        public LessOrEqualThanExpression(Expression expr1, Expression expr2)
        {
            Expr1 = expr1;
            Expr2 = expr2;
            Type = EXPRESSION_TYPE.LESS_OR_EQUAL;
        }
        public override string Dump()
        {

            string result = "";
            result += Expr1.Dump();
            result += " <= ";
            result += Expr2.Dump();
            return result;
        }

        public override AstType Execute()
        {
            var result1 = Expr1.Execute();
            var result2 = Expr2.Execute();
            return result1.SmallerOrEqualThan(result2);
        }

        Expression Expr1;
        Expression Expr2;
    }

    public class BiggerOrEqualThanExpression : Expression
    {
        public BiggerOrEqualThanExpression(Expression expr1, Expression expr2)
        {
            Expr1 = expr1;
            Expr2 = expr2;
            Type = EXPRESSION_TYPE.BIGGER_OR_EQUAL;
        }
        public override string Dump()
        {

            string result = "";
            result += Expr1.Dump();
            result += " >= ";
            result += Expr2.Dump();
            return result;
        }

        public override AstType Execute()
        {
            var result1 = Expr1.Execute();
            var result2 = Expr2.Execute();
            return result1.LargerOrEqualThan(result2);
        }

        Expression Expr1;
        Expression Expr2;
    }
}
