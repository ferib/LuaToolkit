using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace LuaToolkit.Ast
{
    public enum STATEMENT_TYPE
    {
        VAR, IF, IF_ELSE, FUNCTION_DEF
    }

    public enum VAL_TYPE
    {
        VOID, INT, DOUBLE, CHAR, STRING, BOOL
    }

    [System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit)]
    public struct AstType
    {
        public AstType()
        {
            Assigned = false;
            Void = false;
            Int = 0;
        }


        [System.Runtime.InteropServices.FieldOffset(0)]
        public bool Assigned;

        [System.Runtime.InteropServices.FieldOffset(1)]
        public bool Void;

        [System.Runtime.InteropServices.FieldOffset(2)]
        public VAL_TYPE Type;

        [System.Runtime.InteropServices.FieldOffset(6)]
        public int Int;

        [System.Runtime.InteropServices.FieldOffset(6)]
        public double Double;

        [System.Runtime.InteropServices.FieldOffset(6)]
        public char Char;

        //[System.Runtime.InteropServices.FieldOffset(3)]
        //public string String;

        [System.Runtime.InteropServices.FieldOffset(6)]
        public bool Bool;

        public void Set(bool val)
        {
            Assigned = true;
            Void = false;
            Bool = val;
            Type = VAL_TYPE.BOOL;
        }

        public void Set(int val)
        {
            Assigned = true;
            Void = false;
            Int = val;
            Type = VAL_TYPE.INT;
        }

        public void Set(double val)
        {
            Assigned = true;
            Void = false;
            Double = val;
            Type = VAL_TYPE.DOUBLE;
        }
        public void Set(char val)
        {
            Assigned = true;
            Void = false;
            Char = val;
            Type = VAL_TYPE.CHAR;
        }
        public void Set(string val)
        {
            Assigned = true;
            Void = false;
            // String = val;
            Type = VAL_TYPE.STRING;
        }
        public void SetVoid()
        {
            Assigned = true;
            Void = true;
            Type = VAL_TYPE.VOID;
        }
    }

    public class TypeCreator
    {
        public static AstType CreateBool(bool val)
        {
            var result = new AstType();
            result.Set(val);
            return result;
        }
        public static AstType CreateInt(int val)
        {
            var result = new AstType();
            result.Set(val);
            return result;
        }
        public static AstType CreateDouble(double val)
        {
            var result = new AstType();
            result.Set(val);
            return result;
        }
        public static AstType CreateChar(char val)
        {
            var result = new AstType();
            result.Set(val);
            return result;
        }
        public static AstType CreateString(string val)
        {
            var result = new AstType();
            result.Set(val);
            return result;
        }
        public static AstType CreateVoid()
        {
            var result = new AstType();
            result.SetVoid();
            return result;
        }
    }

    public abstract class Statement
    {
        public abstract string Dump();

        public abstract AstType Execute();
    }

    public abstract class Expression
    {
        public abstract string Dump();

        public abstract AstType Execute();
    }

    public class EmptyStatement : Statement
    {
        public override string Dump()
        {
            Debug.Assert(false, "There should never be an empty statement");
            return "Empty" + StringUtil.NewLineChar;
        }

        public override AstType Execute()
        {
            Debug.Assert(false, "There should never be an empty statement");
            return new AstType();
        }
    }

    public class StatementList : Statement
    {
        public StatementList()
        {
            Statements = new List<Statement> ();
        }
        public StatementList(List<Statement> statements)
        {
            foreach(var statement in statements)
            {
                Statements.Add(statement);
            }
        }
        public void Add(Statement statement)
        {
            Statements.Add(statement);
        }
        public override string Dump()
        {
            string result = "";
            foreach(var statement in Statements)
            {
                result += statement.Dump();
            }
            return result;
        }

        public override AstType Execute()
        {
            AstType result = new AstType();
            foreach (var statement in Statements)
            {
               result = statement.Execute();
            }
            return result;
        }

        public List<Statement> Statements;
    }

    public class Variable : Expression
    {
        public Variable(string name)
        {
            Name = name;
            Content = new AstType();
        }
        public Variable(string name, AstType content)
        {
            Name = name;
            Content = content;
        }

        public override string Dump()
        {
            return Name;
        }

        public override AstType Execute()
        {
            return Content;
        }

        public string Name;
        public AstType Content;
    }

    public class Constant : Expression
    {
        public Constant(AstType content)
        {
            Content = content;
        }

        public override string Dump()
        {
            switch (Content.Type) {
                case VAL_TYPE.INT:
                    return Content.Int.ToString();
                case VAL_TYPE.DOUBLE:
                    return Content.Double.ToString();
                case VAL_TYPE.CHAR:
                    return Content.Char.ToString();
                //case VAL_TYPE.STRING:
                  //  return Content.String.ToString();
                case VAL_TYPE.BOOL:
                    return Content.Bool.ToString();
                case VAL_TYPE.VOID:
                    Debug.Assert(false, "Cannot create a constant of void type");
                    return "void";
                default:
                    Debug.Assert(false, "Unsupported Constant type '" 
                        + Content.Type.ToString() + "'");
                    return "";
            }
        }

        public override AstType Execute()
        {
            return Content;
        }

        public AstType Content;
    }
}
