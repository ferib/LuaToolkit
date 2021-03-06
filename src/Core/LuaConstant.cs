using LuaToolkit.Models;

namespace LuaToolkit.Core
{
    public abstract class LuaConstant
    {
        public LuaType Type
        {
            get;
            set;
        }

        public override abstract string ToString();
    }

    public class LuaConstant<T> : LuaConstant
    {
        public T Value
        {
            get;
            set;
        }

        protected LuaConstant(LuaType type, T value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class NilConstant : LuaConstant<object>
    {
        public NilConstant() : base(LuaType.Nil, null)
        { }

        public override string ToString()
        {
            return "nil";
        }
    }

    public class BoolConstant : LuaConstant<bool>
    {
        public BoolConstant(bool value) : base(LuaType.Bool, value)
        { }

        public override string ToString()
        {
            return Value ? "true" : "false";
        }
    }

    public class NumberConstant : LuaConstant<double>
    {
        public NumberConstant(double value) : base(LuaType.Number, value)
        { }
    }

    public class StringConstant : LuaConstant<string>
    {
        public StringConstant(string value) : base(LuaType.String, value)
        { }

        public override string ToString()
        {
            // substring to avoid printing out NULL character
            // TODO: strip special characters
            // string safeValue = Value.Replace("\\", "\\\\"); // figure this one out
            if (Value.Contains("\""))
                return '\'' + Value.Substring(0, Value.Length - 1) + '\'';
            else
                return '\"' + Value.Substring(0, Value.Length - 1) + '\"';
        }
    }

    // PrototypeConstant is used for decoding upvalues
    public class PrototypeConstant : LuaConstant<string>
    {
        public PrototypeConstant(string name) : base(LuaType.String, name)
        { }

        public override string ToString()
        {
            string[] split = Value.Split('(');
            if(split.Length > 1)
                return Value.Split('(')[0];
            return Value;
        }
    }

}
