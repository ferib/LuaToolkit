using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LuaToolkit.Ast
{
    public class NewTableExpression : Expression
    {
        public NewTableExpression(int size)
        {
            Size = size;
            Type = EXPRESSION_TYPE.TABLE_NEW;
        }
        public override string Dump()
        {
            return "{}";
        }

        public override AstType Execute()
        {
            // TODO Execute Tables
            return TypeCreator.CreateNil();
        }

        int Size;
    }

    public class GetTableExpression : Expression
    {
        public GetTableExpression(Variable table, Expression index)
        {
            Table = table;
            TableIndex = index;
        }
        public override string Dump()
        {
            string result = "";
            result += Table.Dump();
            result += "[" + TableIndex.Dump() + "]";
            return result ;
        }

        public override AstType Execute()
        {
            // TODO Execute Tables
            return TypeCreator.CreateNil();
        }

        Variable Table;
        Expression TableIndex;
    }

    public class SetTableExpression : Expression
    {
        public SetTableExpression(Variable table, Expression index)
        {
            Table = table;
            TableIndex = index;
        }
        public override string Dump()
        {
            string result = "";
            result += Table.Dump();
            result += "[" + TableIndex.Dump() + "]";
            return result;
        }

        public override AstType Execute()
        {
            // TODO Execute Tables
            return TypeCreator.CreateNil();
        }

        Variable Table;
        Expression TableIndex;
    }

    public class SetListExpression : Expression
    {
        public SetListExpression(List<Expression> list)
        {
            List = list;
            Type = EXPRESSION_TYPE.LIST_SET;
        }

        public override string Dump()
        {
            string result = "";
            result += "{ ";
            foreach(Expression e in List)
            {
                result += e.Dump();
                if(List.IndexOf(e) != List.Count - 1)
                {
                    result += ", ";
                }
            }
            result += " }";
            return result;
        }

        public override AstType Execute()
        {
            // TODO Execute Tables
            return TypeCreator.CreateNil();
        }

        List<Expression> List;
    }
}
