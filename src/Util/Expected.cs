using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Util
{
    public class Expected<T>
    {
        public Expected(T val){
            Value = val;
            Error = false;
        }

        public Expected(string errorMsg)
        {
            Error = true;
            ErrorMsg = errorMsg;
        }

        public bool HasError()
        {
            return Error;
        }

        public string GetError()
        {
            return ErrorMsg;
        }

        public static implicit operator Expected<T>(T val) => new Expected<T>(val);


        bool Error = true;
        public T Value;
        string ErrorMsg = "";
    }
}
