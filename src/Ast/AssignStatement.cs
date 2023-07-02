using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Ast
{
    public class AssignStatement : Statement
    {
        public AssignStatement(Variable var, Expression expression)
        {
            Var = var;
            Expression = expression;
            Type = STATEMENT_TYPE.ASSIGN;
        }

        public AssignStatement(Global glob, Expression expression)
        {
            Global = glob;
            Expression = expression;
            Type = STATEMENT_TYPE.ASSIGN;
        }
        public AssignStatement(List<Variable> varlist, Expression expression)
        {
            VarList = varlist;
            Expression = expression;
            Type = STATEMENT_TYPE.ASSIGN;
        }

        public AssignStatement(SetTableExpression setTable, Expression expression)
        {
            SetTable = setTable;
            Expression = expression;
            Type = STATEMENT_TYPE.ASSIGN;
        }

        public AssignStatement(Upvalue upval, Expression expression)
        {
            Upvalue = upval;
            Expression = expression;
            Type = STATEMENT_TYPE.ASSIGN;
        }

        public override string Dump(string linePrefix = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(linePrefix);
            if(Var != null)
            {
                if(Init)
                {
                    sb.Append("local ");
                }
                sb.Append(Var.Dump());
            } 
            else if(Global != null)
            {
                sb.Append(Global.Dump());
            }
            else if(VarList != null)
            {
                foreach(var var in VarList)
                {
                    sb.Append(var.Dump());
                    if(VarList.IndexOf(var) != VarList.Count -1)
                    {
                        sb.Append(", ");
                    }
                }
            }
            else if (SetTable != null)
            {
                sb.Append(SetTable.Dump());
            }
            else if (Upvalue != null)
            {
                sb.Append(Upvalue.Dump());
            }
            else
            {
                Debug.Assert(false, "AssignStatement needs var/varlist or global");
            }
            sb.Append(" = ").AppendLine(Expression.Dump());
            return sb.ToString();
        }

        public override AstType Execute()
        {
            // Todo add support for all types.
            Var.Content = Expression.Execute();
            return TypeCreator.CreateBool(Var.Content.Assigned);
        }

        public Variable Var;
        public Global Global;
        public List<Variable> VarList;
        public SetTableExpression SetTable;
        public Upvalue Upvalue;

        public Expression Expression;
        public bool Init
        {
            get;
            set;
        }
    }
}
