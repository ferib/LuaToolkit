using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Models;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class LOPlugin
    {
        public LuaDecoder Decoder;
        public string Description;

        public LOPlugin(ref LuaDecoder decoder, string desc)
        {
            this.Decoder = decoder;
            this.Description = desc;
        }
    }
}
