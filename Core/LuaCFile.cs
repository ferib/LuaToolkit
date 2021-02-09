using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Models;

namespace LuaSharpVM.Core
{
    public class LuaCFile
    {
        public byte Format;
        public bool BigEndian;
        public byte IntSize;
        public byte SizeTSize;
        public byte InstructionSize;
        public byte LuaNumberSize;
        public byte Integral;

        public byte[] Buffer;
        public LuaFunction Function;

        public LuaCFile(byte[] buffer)
        {
            this.Buffer = buffer;
        }
    }
}
