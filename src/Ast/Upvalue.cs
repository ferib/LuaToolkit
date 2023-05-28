using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class Upvalue : Expression
    {
        public Upvalue(string name, AstType value)
        {
            Name = name;
            Content = value;
            Type = EXPRESSION_TYPE.UP_VALUE;
        }
        public override string Dump()
        {
            return Name;
        }

        public override AstType Execute()
        {
            return Content;
        }

        string Name;
        AstType Content;
    }
}
