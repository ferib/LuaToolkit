using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Ast
{
    public class Convertor<T> where T : Statement
    {
        static public Expected<T> Convert(Statement statement)
        {
            if(statement == null)
            {
                return new Expected<T>("Cannot convert nullptr");
            }
            if(typeof(T) == statement.GetType())
            {
                return statement as T;
            }

            return new Expected<T>("Cannot convert " +
                statement.GetType().ToString() + " to " + typeof(T));
        }

        static public bool IsType(Statement statement)
        {
            return typeof(T) == statement.GetType();
        }
    }
}
