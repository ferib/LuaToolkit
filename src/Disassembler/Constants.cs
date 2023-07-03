using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Disassembler
{
    public abstract class ByteConstant
    {

        public LuaType Type
        {
            get;
            set;
        }

        public abstract string Dump();
    }

    public class ByteConstant<T> : ByteConstant
    {
        protected ByteConstant(LuaType type, T value)
        {
            Type = type;
            Value = value;
        }

        public T Value
        {
            get;
            set;
        }

        public override string Dump()
        {
            return Value.ToString();
        }
    }

    public class NilByteConstant : ByteConstant<object>
    {
        public NilByteConstant() : base(LuaType.Nil, null)
        {
        }
        public override string Dump()
        {
            return "nil";
        }
    }

    public class BoolByteConstant : ByteConstant<bool>
    {
        public BoolByteConstant(bool value) : base(LuaType.Bool, value)
        {
        }
        public override string Dump()
        {
            return Value ? "true" : "false";
        }
    }

    public class NumberByteConstant : ByteConstant<double>
    {
        public NumberByteConstant(double value) : base(LuaType.Number, value)
        {
        }
    }

    public class StringByteConstant : ByteConstant<string>
    {
        public StringByteConstant(string value) : base(LuaType.String, value)
        { 
        }

        public override string Dump()
        {
            return Value;
        }
    }
}
