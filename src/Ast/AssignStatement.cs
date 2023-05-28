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

        public override string Dump()
        {
            string result = "";
            if(Var != null)
            {
                result += Var.Dump();
            } 
            else if(Global != null)
            {
                result += Global.Dump();
            }
            else if(VarList != null)
            {
                foreach(var var in VarList)
                {
                    result += var.Dump();
                    if(VarList.IndexOf(var) != VarList.Count -1)
                    {
                        result += ", ";
                    }
                }
            }
            else if (SetTable != null)
            {
                result += SetTable.Dump();
            }
            else if (Upvalue != null)
            {
                result += Upvalue.Dump();
            }
            else
            {
                Debug.Assert(false, "AssignStatement needs var/varlist or global");
            }
            result += " = " + Expression.Dump() + StringUtil.NewLineChar;

            return result;
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
    }
}
