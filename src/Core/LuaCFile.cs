using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit.Models;

namespace LuaToolkit.Core
{
    public class LuaCFile
    {
        public byte Format = 0;
        public bool BigEndian = false;
        public byte IntSize = 4;
        public byte SizeTSize = 8;
        public byte InstructionSize = 4;
        public byte LuaNumberSize = 8;
        public byte Integral = 0;

        public byte[] Buffer;
        public LuaFunction Function;

        public LuaCFile(byte[] buffer)
        {
            this.Buffer = buffer;
        }
    }
}
