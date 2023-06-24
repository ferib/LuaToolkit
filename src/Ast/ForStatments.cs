using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class ForStatement : Statement
    {
        public ForStatement()
        {
            Type = STATEMENT_TYPE.FOR;
        }

        public ForStatement(Expression loopVar, Expression initialVal,
            Expression limit, Expression step, Statement body) : base()
        {
            LoopVariable = loopVar;
            InitialVal = initialVal;
            Limit = limit;
            Step = step;
            Body = body;
        }

        public override string Dump()
        {
            string result = "";
            result += "for ";
            result += LoopVariable.Dump();
            result += " = ";
            result += InitialVal.Dump();
            result += ", ";
            result += Limit.Dump();
            if(Step != null)
            {
                result += ", ";
                result += Step.Dump();
            }
            result += " do" + StringUtil.NewLineChar;
            result += Body.Dump();
            result += "end" + StringUtil.NewLineChar;

            return result;
        }

        // TODO currently only for loops with ints are supported
        public override AstType Execute()
        {
            var init = InitialVal.Execute();
            var step = Step.Execute();
            var limit = Limit.Execute();
            for(;init.Int < limit.Int; init.Int += step.Int)
            {
                Body.Execute();
            }
            return new AstType();
        }

        public Expression LoopVariable;
        public Expression InitialVal;
        public Expression Limit;
        public Expression Step;
        public Statement Body;
    }

    public class TForStatement : Statement
    {
        public TForStatement(List<Expression> loopVars, 
            Expression iteratorFunction, Statement body) 
        {
            LoopVariables = loopVars;
            IteratorFunction = iteratorFunction;
            Body = body;
        }
        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("for ");
            foreach(var expr in LoopVariables)
            {
                sb.Append(expr.Dump());
                if(LoopVariables.IndexOf(expr) < LoopVariables.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(" in ").Append(IteratorFunction.Dump()).AppendLine(" do ");
            sb.Append(Body.Dump());
            sb.AppendLine("end");
            return sb.ToString();
        }

        public override AstType Execute()
        {
            // TODO implement
            return new AstType();
        }

        List<Expression> LoopVariables;
        Expression IteratorFunction;
        Statement Body;
    }
}
