using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Models;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscator.Plugin
{
    public class LOEncrypt : LOPlugin
    {
        // Encrypt a given stub using a XOR to break basic tools

        static string desc = "TODO";
        public LOEncrypt(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }
        // TODO: create a hash table for each function name
        //       create a custom hash function to funcname -> hash
        //       have another custom func handle the hash calling

        public override void Obfuscate(int lvl)
        {
            throw new NotImplementedException();
        }
    }
}
