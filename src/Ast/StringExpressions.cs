using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Ast
{
    public class LenExpression : Expression
    {
        public LenExpression(Expression expr)
        {
            Expr = expr;
            Type = EXPRESSION_TYPE.LEN;
        }

        public override string Dump()
        {
            var result = "";
            result += "#" + Expr.Dump();
            return result;
        }

        public override AstType Execute()
        {
            var res = Expr.Execute();
            
            switch (res.Type)
            {
                case VAL_TYPE.STRING:
                    return TypeCreator.CreateInt(res.String.Length);
                default:
                    return TypeCreator.CreateNil();
            }
        }

        Expression Expr;
    }

    public class ConcatExpression : Expression
    {
        public ConcatExpression(Expression expr1, Expression expr2)
        {
            Exprs = new List<Expression>() { expr1, expr2 };
            Type = EXPRESSION_TYPE.CONCAT;
        }

        public ConcatExpression(List<Expression> exprs)
        {
            Exprs = exprs;
            Type = EXPRESSION_TYPE.CONCAT;
        }

        public override string Dump()
        {
            var result = "";
            foreach(Expression expr in Exprs)
            {
                result += expr.Dump();
                if(Exprs.IndexOf(expr) != Exprs.Count - 1)
                {
                    result += " .. ";
                }
            }
            return result;
        }

        public override AstType Execute()
        {
            var resString = "";
            foreach(var expr in Exprs)
            {
                var res = expr.Execute();
                if(res.Type != VAL_TYPE.STRING)
                {
                    Debug.Assert(false, "Only strings can be concatenated");
                    continue;
                }
                resString += res.String;

            }
            return TypeCreator.CreateString(resString);
        }

        List<Expression> Exprs;
    }
}
