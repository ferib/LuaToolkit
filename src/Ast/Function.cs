using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Ast
{
    public class FunctionStatement : Statement
    {
        public FunctionStatement(string name, StatementList statements)
        {
            Name = name;
            StatementList = statements;
        }
        public override string Dump()
        {
            Debug.Assert(false, "A function definition should never be dumped");
            string result = "";
            result += "function " + Name + "()" + StringUtil.NewLineChar;
            result += StatementList.Dump();
            result += "end";
            return result;
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
        }
        public override string Dump()
        {
            string result = "";
            result += "function " + Name + "()" + StringUtil.NewLineChar;
            result += StatementList.Dump();
            result += "end";
            return result;
        }

        public override AstType Execute()
        {
            FunctionTable.Instance.AddFunction(Name, new FunctionStatement(Name, this.StatementList));
            return new AstType();
        }

        public string Name;
        public StatementList StatementList;
    }


}
