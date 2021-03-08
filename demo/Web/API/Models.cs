using System;
using System.Collections.Generic;
using System.Text;

namespace Web.API
{
    public class APIResponse<T>
    {
        public string status;
        public string message;
        public T data = Activator.CreateInstance<T>();
    }

    public class ResponseDecompiler
    {
        public string decompiled;
    }

    public class ResponseHighlighter
    {
        public string highlighted;
        // TODO: list of character highlights where? [color, [0,1]]
    }

    public class ResponseBeautifier
    {
        public string beautified;
    }
}
