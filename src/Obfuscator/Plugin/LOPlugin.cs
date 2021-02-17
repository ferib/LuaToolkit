using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Models;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscator.Plugin
{
    public abstract class LOPlugin
    {
        public LuaDecoder Decoder;
        public string Description;
        public List<string> Functions;
        public List<int> Levels;

        public LOPlugin(ref LuaDecoder decoder, string desc)
        {
            this.Decoder = decoder;
            this.Description = desc;
            this.Functions = new List<string>();
            this.Levels = new List<int>();
        }

        public abstract void Obfuscate();

        public abstract string GetName();

    }
}
