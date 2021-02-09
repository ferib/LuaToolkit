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

        public void WriteHeader()
        {

        }

        public void EncodeFunctionblock(LuaFunction function)
        {

        }

        public void WriteInstructions(List<LuaInstruction> instructions)
        {

        }

        public void WriteConstants(List<LuaConstant> constants)
        {

        }

        public void WriteFunctions(List<LuaFunction> functions)
        {

        }

        public void WriteDebugLines(List<int> lines)
        {

        }

        public void WriteDebugLocals(List<LuaLocal> locals)
        {

        }

        public void WriteDebugUpvals(List<string> upvals)
        {

        }

        public void SetByte(byte d)
        {

        }

        public void SetInt(int d)
        {

        }

        public void SetFloat(float d)
        {

        }

        public void SetFloat2(double d)
        {

        }

        public void SetLong(long d)
        {

        }

        public void SetString(string str)
        {

        }


        // TODO: the reverse of that LuaDecoder does in order to reconstruct a LuaC file
    }
}
