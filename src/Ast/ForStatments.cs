using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class ForStatment : Statement
    {
        public ForStatment()
        {
            Type = STATEMENT_TYPE.FOR;
        }

        public ForStatment(Expression initialVal, Expression limit, Expression step, Statement body)
        {
            InitialVal = initialVal;
            Limit = limit;
            Step = step;
            Body = body;
        }

        public ForStatment(Expression initialVal, Expression limit, Statement body)
        {
            InitialVal = initialVal;
            Limit = limit;
            Body = body;
        }

        public override string Dump()
        {
            string result = "";
            result += "for ";
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

        // TODO currently only for loops met ints are supported
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

        public Expression InitialVal;
        public Expression Limit;
        public Expression Step;
        public Statement Body;
    }
}
