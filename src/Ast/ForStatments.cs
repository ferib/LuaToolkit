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

        public override string Dump(string linePrefix = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(linePrefix).Append("for ").Append(LoopVariable.Dump())
                .Append(" = ").Append(InitialVal.Dump()).Append(", ")
                .Append(Limit.Dump());
            if (Step != null)
            {
                sb.Append(", ").Append(Step.Dump());
            }
            sb.Append(" do").AppendLine();
            sb.Append(Body.Dump(linePrefix + "\t"));
            sb.Append(linePrefix).Append("end").AppendLine();
            return sb.ToString();
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
        public override string Dump(string linePrefix = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(linePrefix).Append("for ");
            foreach(var expr in LoopVariables)
            {
                sb.Append(expr.Dump());
                if(LoopVariables.IndexOf(expr) < LoopVariables.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(" in ").Append(IteratorFunction.Dump()).AppendLine(" do ");
            sb.Append(Body.Dump(linePrefix + "\t"));
            sb.Append(linePrefix).AppendLine("end");
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
