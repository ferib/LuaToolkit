using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class Upvalue : Expression
    {

        public Upvalue(string name)
        {
            Name = name;
            Type = EXPRESSION_TYPE.UP_VALUE;
        }
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
            // For executing the upval has to be looked up in the parent function
            if(!Content.Assigned)
            {
                return TypeCreator.CreateNil();
            }
            return Content;
        }

        string Name;
        AstType Content;
    }
}
