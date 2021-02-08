using LuaSharpVM.Models;

namespace LuaSharpVM.Core
{
    public abstract class LuaConstant
    {
        public LuaType Type
        {
            get;
            protected set;
        }

        public override abstract string ToString();
    }

    public class LuaConstant<T> : LuaConstant
    {
        public T Value
        {
            get;
            private set;
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
            return '\"' + Value.Substring(0, Value.Length - 1) + '\"';
        }
    }
}
