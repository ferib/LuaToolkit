﻿using LuaToolkit.Ast;
using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace LuaToolkit.Ast
{
    public enum STATEMENT_TYPE
    {
        EMPTY, ASSIGN, IF, IF_ELSE, ELSEIF, ELSEIF_ELSE, FUNCTION_DEF, LIST, JMP, FUNCTION, RETURN,
        FOR
    }

    public enum EXPRESSION_TYPE
    {
        EMPTY, CONST, VAR, NOT, OR, AND, EQ, NOT_EQ, LESS_THAN, LESS_OR_EQUAL, 
        BIGGER_THAN, BIGGER_OR_EQUAL,
        ADD, SUB, MUL, DIV, POW, NEG
    }

    public enum VAL_TYPE
    {
        NIL, INT, DOUBLE, CHAR, STRING, BOOL
    }

    [System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit)]
    public struct AstType
    {
        public AstType()
        {
            Assigned = false;
            Nil = false;
            Int = 0;
        }

        [FieldOffset(0)]
        public bool Assigned;

        [FieldOffset(1)]
        public bool Nil;

        [FieldOffset(2)]
        public VAL_TYPE Type;

        [FieldOffset(6)]
        public int Int;

        [FieldOffset(6)]
        public double Double;

        [FieldOffset(6)]
        public char Char;

        [FieldOffset(6)]
        public bool Bool;

        // [FieldOffset(15)]
        // public string String;

        public void Set(bool val)
        {
            Assigned = true;
            Nil = false;
            Bool = val;
            Type = VAL_TYPE.BOOL;
        }

        public void Set(int val)
        {
            Assigned = true;
            Nil = false;
            Int = val;
            Type = VAL_TYPE.INT;
        }

        public void Set(double val)
        {
            Assigned = true;
            Nil = false;
            Double = val;
            Type = VAL_TYPE.DOUBLE;
        }
        public void Set(char val)
        {
            Assigned = true;
            Nil = false;
            Char = val;
            Type = VAL_TYPE.CHAR;
        }
        public void Set(string val)
        {
            Assigned = true;
            Nil = false;
            // String = val;
            Type = VAL_TYPE.STRING;
        }
        public void SetNil()
        {
            Assigned = true;
            Nil = true;
            Type = VAL_TYPE.NIL;
        }

        public AstType Equals(AstType second)
        {
            Debug.Assert(Type == second.Type, "Comparing values from different types");
            AstType result = new AstType();
            switch (Type)
            {
                case VAL_TYPE.INT:
                    result.Set(Int == second.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(Double == second.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(Char == second.Char);
                    break;
                case VAL_TYPE.BOOL:
                    result.Set(Bool == second.Bool);
                    break;
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
        }

        public AstType SmallerThan(AstType second)
        {
            Debug.Assert(Type == second.Type, "Comparing values from different types");
            AstType result = new AstType();
            switch (Type)
            {
                case VAL_TYPE.INT:
                    result.Set(Int < second.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(Double < second.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(Char < second.Char);
                    break;
                case VAL_TYPE.BOOL:
                    result.Set(Int < second.Int);
                    break;
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
        }

        public AstType LargerThan(AstType second)
        {
            Debug.Assert(Type == second.Type, "Comparing values from different types");
            AstType result = new AstType();
            switch (Type)
            {
                case VAL_TYPE.INT:
                    result.Set(Int > second.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(Double > second.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(Char > second.Char);
                    break;
                case VAL_TYPE.BOOL:
                    result.Set(Int > second.Int);
                    break;
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
        }

        public AstType SmallerOrEqualThan(AstType second)
        {
            Debug.Assert(Type == second.Type, "Comparing values from different types");
            AstType result = new AstType();
            switch (Type)
            {
                case VAL_TYPE.INT:
                    result.Set(Int <= second.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(Double <= second.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(Char <= second.Char);
                    break;
                case VAL_TYPE.BOOL:
                    result.Set(Int <= second.Int);
                    break;
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
        }

        public AstType LargerOrEqualThan(AstType second)
        {
            Debug.Assert(Type == second.Type, "Comparing values from different types");
            AstType result = new AstType();
            switch (Type)
            {
                case VAL_TYPE.INT:
                    result.Set(Int >= second.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(Double >= second.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(Char >= second.Char);
                    break;
                case VAL_TYPE.BOOL:
                    result.Set(Int >= second.Int);
                    break;
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
        }

        public static AstType operator+(AstType first, AstType second)
        {
            Debug.Assert(first.Type == second.Type, "Adding values from different types");
            AstType result = new AstType();
            switch (first.Type)
            {
                case VAL_TYPE.INT:
                    result.Set(first.Int + second.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(first.Double + second.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(first.Char + second.Char);
                    break;
                case VAL_TYPE.BOOL:
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + first.Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
        }

        public static AstType operator -(AstType first, AstType second)
        {
            Debug.Assert(first.Type == second.Type, "Subtracting values from different types");
            AstType result = new AstType();
            switch (first.Type)
            {
                case VAL_TYPE.INT:
                    result.Set(first.Int - second.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(first.Double - second.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(first.Char - second.Char);
                    break;
                case VAL_TYPE.BOOL:
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + first.Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
        }

        public static AstType operator -(AstType val)
        {
            AstType result = new AstType();
            switch (val.Type)
            {
                case VAL_TYPE.INT:
                    result.Set(-val.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(-val.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(-val.Char);
                    break;
                case VAL_TYPE.BOOL:
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + val.Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
        }

        public static AstType operator *(AstType first, AstType second)
        {
            Debug.Assert(first.Type == second.Type, "Subtracting values from different types");
            AstType result = new AstType();
            switch (first.Type)
            {
                case VAL_TYPE.INT:
                    result.Set(first.Int * second.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(first.Double * second.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(first.Char * second.Char);
                    break;
                case VAL_TYPE.BOOL:
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + first.Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
        }

        public static AstType operator /(AstType first, AstType second)
        {
            Debug.Assert(first.Type == second.Type, "Subtracting values from different types");
            AstType result = new AstType();
            switch (first.Type)
            {
                case VAL_TYPE.INT:
                    result.Set(first.Int / second.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(first.Double / second.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(first.Char / second.Char);
                    break;
                case VAL_TYPE.BOOL:
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + first.Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
        }

        public static AstType Power(AstType first, AstType second)
        {
            Debug.Assert(first.Type == second.Type, "Subtracting values from different types");
            AstType result = new AstType();
            switch (first.Type)
            {
                case VAL_TYPE.INT:
                    result.Set(Convert.ToInt32( Math.Pow(first.Int, second.Int)));
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(Math.Pow(first.Double, second.Double));
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(Convert.ToChar( Math.Pow(first.Char, second.Char)));
                    break;
                case VAL_TYPE.BOOL:
                case VAL_TYPE.NIL:
                default:
                    Debug.Assert(false, "Type: '" + first.Type.ToString() + "' Not supported");
                    return result;
            }
            return result;
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
        public static AstType CreateNil()
        {
            var result = new AstType();
            result.SetNil();
            return result;
        }
    }

    public abstract class Statement
    {
        public abstract string Dump();

        public abstract AstType Execute();

        public Statement GetNextStatement()
        {
            if(Parent == null)
            {
                return null;
            }
            var index = Parent.Statements.IndexOf(this);
            if(index == Parent.Statements.Count - 1)
            {
                return null;
            }
            return Parent.Statements[index + 1];
        }

        public STATEMENT_TYPE Type = STATEMENT_TYPE.EMPTY;

        public StatementList Parent;
    }

    public abstract class Expression
    {
        public abstract string Dump();

        public abstract AstType Execute();

        public EXPRESSION_TYPE Type = EXPRESSION_TYPE.EMPTY;
    }

    public class EmptyStatement : Statement
    {
        public override string Dump()
        {
            // Debug.Assert(false, "There should never be an empty statement");
            return "Empty" + StringUtil.NewLineChar;
        }

        public override AstType Execute()
        {
            // Debug.Assert(false, "There should never be an empty statement");
            return new AstType();
        }
    }

    public class EmptyExpression : Expression
    {
        public override string Dump()
        {
            // Debug.Assert(false, "There should never be an empty expression");
            return "Empty" + StringUtil.NewLineChar;
        }

        public override AstType Execute()
        {
            // Debug.Assert(false, "There should never be an empty expression");
            return new AstType();
        }
    }

    public class StatementList : Statement
    {
        public StatementList()
        {
            Statements = new List<Statement> ();
            Type = STATEMENT_TYPE.LIST;
        }
        public StatementList(List<Statement> statements)
        {
            foreach(var statement in statements)
            {
                Add(statement);
            }
        }
        public void Add(Statement statement)
        {
            Statements.Add(statement);
            statement.Parent = this;
        }

        public void Insert(int index, Statement statement)
        {
            Statements.Insert(index, statement);
            statement.Parent = this;
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
            Type = EXPRESSION_TYPE.VAR;
        }
        public Variable(string name, AstType content)
        {
            Name = name;
            Content = content;
            Type = EXPRESSION_TYPE.VAR;
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
            Type = EXPRESSION_TYPE.CONST;
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
                case VAL_TYPE.NIL:
                    //Debug.Assert(false, "Cannot create a constant of void type");
                    return "nil";
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

public class JumpStatement : Statement
{
    public JumpStatement(Statement statement)
    {
        Statement = statement;
        Type = STATEMENT_TYPE.JMP;
    }

    public override string Dump()
    {
        return "Jump '" + Statement.Dump() + "'";
    }

    public override AstType Execute()
    {
        return Statement.Execute();
    }

    public Statement Statement;
}
