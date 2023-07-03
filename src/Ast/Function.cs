using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace LuaToolkit.Ast
{
    public class FunctionStatement : Statement
    {
        public FunctionStatement(string name, StatementList statements)
        {
            Name = name;
            StatementList = statements;
            Type = STATEMENT_TYPE.FUNCTION;
        }
        public override string Dump(string linePrefix="")
        {
            Debug.Assert(false, "A function definition should never be dumped");
            StringBuilder sb = new StringBuilder();
            sb.Append(linePrefix).Append("function ").Append(Name).Append("()").AppendLine();
            sb.Append(StatementList.Dump(linePrefix + "\t"));
            sb.Append(linePrefix).Append("end");
            return sb.ToString();
        }

        public override AstType Execute()
        {
            return StatementList.Execute();
        }
        public string Name;
        public StatementList StatementList;
    }

    public sealed class FunctionTable
    {
        private static FunctionTable instance = new FunctionTable();

        public void AddFunction(string name, FunctionStatement function)
        {
            functions.Add(name, function);
        }

        public static FunctionTable Instance
        {
            get
            {
                return instance;
            }
        }
        Dictionary<string, FunctionStatement> functions = new Dictionary<string, FunctionStatement>();
    }

    public class FunctionDefinitionStatement : Statement
    {
        public FunctionDefinitionStatement(string name, StatementList statements)
        {
            Name = name;
            StatementList = statements;
            VarArg = false;
            Type = STATEMENT_TYPE.FUNCTION_DEF;
        }
        public override string Dump(string linePrefix = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(linePrefix).Append("function ").Append(Name).Append("(");
            // Todo arguments
            if(VarArg)
            {
                sb.Append("...");
            }
            sb.Append(")").AppendLine();
            sb.Append(StatementList.Dump(linePrefix + "\t"));
            sb.Append("end").AppendLine();
            return sb.ToString();
        }

        public override AstType Execute()
        {
            FunctionTable.Instance.AddFunction(Name, new FunctionStatement(Name, this.StatementList));
            return new AstType();
        }

        public bool VarArg;
        public string Name;
        public StatementList StatementList;
    }

    public class ReturnStatement : Statement
    {
        public ReturnStatement()
        {
            Exprs = new List<Expression>();
            Type = STATEMENT_TYPE.RETURN;
        }

        public ReturnStatement(Expression expr) : this()
        {
            Exprs.Add(expr);
        }

        public ReturnStatement(List<Expression> exprs) : this()
        {
            Exprs.AddRange(exprs);
        }

        public override string Dump(string linePredix="")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(linePredix).Append("return ");
            for(int i = 0; i < Exprs.Count; i++)
            {
                sb.Append(Exprs[i].Dump());
                if(i < Exprs.Count - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.AppendLine();
            return sb.ToString();
        }

        public override AstType Execute()
        {
            // TODO Fix convert to list of results.
            AstType res = new AstType();
            foreach(var expr in Exprs)
            {
                res = expr.Execute();
            }
            return res;
        }
        public List<Expression> Exprs;

    }

    public class CallExpression : Expression
    {
        public CallExpression(string name)
        {
            Name = name;
            Type = EXPRESSION_TYPE.FUNC_CALL;
        }

        public CallExpression(string name, List<string> args)
        {
            Name = name;
            Arguments = args;
            Type = EXPRESSION_TYPE.FUNC_CALL;
        }

        public override string Dump()
        {
            string result = "";
            result += Name + "(";
            foreach(var arg in Arguments)
            {
                result += arg;
                if(Arguments.IndexOf(arg) != Arguments.Count - 1)
                {
                    result += ", ";
                }
            }
            result += ")";

            return result;
        }

        public override AstType Execute()
        {
            return TypeCreator.CreateNil();
        }

        string Name;
        List<string> Arguments;
    }

    public class VarArg : Expression
    {
        public VarArg()
        {
            Type = EXPRESSION_TYPE.VAR_ARG;
        }
        public override string Dump()
        {
            return "...";
        }

        public override AstType Execute()
        {
            // Todo execution of vararg
            return TypeCreator.CreateNil();
        }
    }


}
