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
        public int Level;

        public LOPlugin(ref LuaDecoder decoder, string desc)
        {
            this.Decoder = decoder;
            this.Description = desc;
        }

        public abstract void Obfuscate(int lvl);
    }
}
