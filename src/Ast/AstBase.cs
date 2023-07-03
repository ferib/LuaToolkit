using LuaToolkit.Ast;
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
        EMPTY, ASSIGN, IF, IF_ELSE, ELSEIF, ELSEIF_ELSE, FUNCTION_DEF, LIST, FUNCTION, RETURN,
        FOR, WHILE, REPEAT, EXPR,
        JMP, CMP
    }

    public enum EXPRESSION_TYPE
    {
        EMPTY, CONST, VAR, NOT, OR, AND, EQ, NOT_EQ, LESS_THAN, LESS_OR_EQUAL, 
        BIGGER_THAN, BIGGER_OR_EQUAL, TEST,
        ADD, SUB, MUL, DIV, POW, NEG,
        GLOBAL, VAR_ARG,
        FUNC_CALL,
        LEN, CONCAT,
        TABLE_NEW, TABLE_GET, TABLE_SET, LIST_SET,
        UP_VALUE,
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
            IsString = false;
            Int = 0;
        }

        public static List<string> StringList = new List<string>();

        [FieldOffset(0)]
        public bool Assigned;

        [FieldOffset(1)]
        public bool Nil;

        [FieldOffset(2)]
        public bool IsString;

        [FieldOffset(3)]
        public VAL_TYPE Type;

        [FieldOffset(7)]
        public int Int;

        [FieldOffset(7)]
        public double Double;

        [FieldOffset(7)]
        public char Char;

        [FieldOffset(7)]
        public bool Bool;

        [FieldOffset(8)]
        public int StringIndex;

        public string String
        {
            get
            {
                return StringList[StringIndex];
            }
            set
            {
                StringList[StringIndex] = value;
            }
        }

        public void Set(bool val)
        {
            Assigned = true;
            Nil = false;
            IsString = false;
            Bool = val;
            Type = VAL_TYPE.BOOL;
        }

        public void Set(int val)
        {
            Assigned = true;
            Nil = false;
            IsString = false;
            Int = val;
            Type = VAL_TYPE.INT;
        }

        public void Set(double val)
        {
            Assigned = true;
            Nil = false;
            IsString = false;
            Double = val;
            Type = VAL_TYPE.DOUBLE;
        }
        public void Set(char val)
        {
            Assigned = true;
            Nil = false;
            IsString = false;
            Char = val;
            Type = VAL_TYPE.CHAR;
        }
        public void Set(string val)
        {
            Assigned = true;
            Nil = false;
            IsString = true;
            StringList.Add(val);
            StringIndex = StringList.Count - 1;
            Type = VAL_TYPE.STRING;
        }
        public void SetNil()
        {
            Assigned = true;
            Nil = true;
            IsString = false;
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
                case VAL_TYPE.STRING:
                    result.Set(String == second.String);
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

        public static AstType operator %(AstType first, AstType second)
        {
            Debug.Assert(first.Type == second.Type, "Subtracting values from different types");
            AstType result = new AstType();
            switch (first.Type)
            {
                case VAL_TYPE.INT:
                    result.Set(first.Int % second.Int);
                    break;
                case VAL_TYPE.DOUBLE:
                    result.Set(first.Double % second.Double);
                    break;
                case VAL_TYPE.CHAR:
                    result.Set(first.Char % second.Char);
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
        public abstract string Dump(string linePrefix="");

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

    public class ExpressionStatement : Statement
    {
        public ExpressionStatement(Expression expr)
        {
            Expr = expr;
            Type = STATEMENT_TYPE.EXPR;
        }
        public override string Dump(string linePrefix = "")
        {
            var sb = new StringBuilder();
            sb.Append(linePrefix).Append(Expr.Dump()).AppendLine();
            return sb.ToString();
        }

        public override AstType Execute()
        {
            return Expr.Execute();
        }
        Expression Expr;
    }

    public class EmptyStatement : Statement
    {
        public override string Dump(string linePrefix = "")
        {
            Debug.Assert(false, "There should never be an empty statement");
            StringBuilder sb = new StringBuilder();
            sb.Append(linePrefix).AppendLine("Empty");
            return sb.ToString();
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
            Debug.Assert(false, "There should never be an empty expression");
            StringBuilder sb = new StringBuilder();
            sb.Append("Empty");
            return sb.ToString();
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
        public override string Dump(string linePrefix = "")
        {
            StringBuilder sb = new StringBuilder();
            foreach(var statement in Statements)
            {
                sb.Append(statement.Dump(linePrefix));
            }
            return sb.ToString();
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
            StringBuilder builder = new StringBuilder();
            builder.Append(Name);
            return builder.ToString();
        }

        public override AstType Execute()
        {
            return Content;
        }

        public string Name;
        public AstType Content;
    }

    public class Global : Expression
    {
        public Global(Constant index)
        {
            Index = index;
            Type = EXPRESSION_TYPE.GLOBAL;
        }

        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("_G[").Append(Index.Dump()).Append("]"); ;
            return sb.ToString();
        }

        public override AstType Execute()
        {
            // TODO Wrong implementation
            // Should return value in global array.
            return Index.Execute();
        }

        public Constant Index;
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
                case VAL_TYPE.STRING:
                    return "\"" + Content.String + "\"";
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
    public JumpStatement(Statement statement, bool jumpForward)
    {
        Statement = statement;
        Type = STATEMENT_TYPE.JMP;
        JumpForward = jumpForward;
    }

    public override string Dump(string linePrefix = "")
    {
        return linePrefix + "Jump '" + Statement.Dump() + "'";
    }

    public override AstType Execute()
    {
        return Statement.Execute();
    }

    public Statement Statement;
    public bool JumpForward;
}

public class ConditionStatement : Statement
{
    public ConditionStatement(Expression expr)
    {
        Expr = expr;
        Type = STATEMENT_TYPE.CMP;
    }
    public override string Dump(string linePrefix = "")
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("CMP: ").Append(Expr.Dump());
        return sb.ToString();
    }

    public override AstType Execute()
    {
        return Expr.Execute();
    }

    public Expression Expr;
}

public class CloseStatement : Statement
{
    public override string Dump(string linePrefix = "")
    {
        return linePrefix + "CLOSE" + StringUtil.NewLineChar;
    }

    public override AstType Execute()
    {
        // Should probably do more.
        return TypeCreator.CreateNil();
    }
}
