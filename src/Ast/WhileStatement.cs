using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class WhileStatement : Statement
    {
        public WhileStatement(Expression condition, Statement body)
        {
            Type = STATEMENT_TYPE.WHILE;
            Condition = condition;
            Body = body;
        }
        public override string Dump(string linePrefix = "")
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(linePrefix).Append("while ").Append(Condition.Dump()).AppendLine(" do");
            sb.Append(Body.Dump(linePrefix + "\t"));
            sb.Append(linePrefix).AppendLine("end");
            return sb.ToString();
        }

        public override AstType Execute()
        {
            while (Condition.Execute().Bool)
            {
                Body.Execute();
            }
            return new AstType();
        }

        Expression Condition;
        Statement Body;
    }

    public class RepeatStatement : Statement
    {
        public RepeatStatement(Expression condition, Statement body)
        {
            Type = STATEMENT_TYPE.REPEAT;
            Condition = condition;
            Body = body;
        }
        public override string Dump(string linePrefix = "")
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(linePrefix).AppendLine("repeat");
            sb.Append(Body.Dump(linePrefix + "\t"));
            sb.Append(linePrefix).Append("until ").AppendLine(Condition.Dump());
            return sb.ToString();
        }

        public override AstType Execute()
        {
            do
            {
                Body.Execute();
            } while (Condition.Execute().Bool);

            return new AstType();
        }

        Expression Condition;
        Statement Body;
    }
}
