using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Models;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Emulator;

namespace LuaSharpVM.Disassembler
{
    public class LuaEncoder
    {
        public LuaFunction Lua;
        private int Index;

        // NOTE: turns a Decoded Lua file back to its bytecode
        public LuaEncoder(LuaFunction lua)
        {
            this.Lua = lua;
        }

        // TODO: the reverse of that LuaDecoder does in order to reconstruct a LuaC file
    }
}
