using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Models;

namespace LuaSharpVM.Core
{
    public class LuaCFile
    {
        public bool BigEndian;
        public int IntSize;
        public int SizeT;

        public byte[] Buffer;
        public LuaFunction Function;

        public LuaCFile(byte[] buffer)
        {
            this.Buffer = buffer;
        }
    }
}
