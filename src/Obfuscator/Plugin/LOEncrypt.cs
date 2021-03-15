using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit.Models;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;

namespace LuaToolkit.Obfuscator.Plugin
{
    public class LOEncrypt : LOPlugin
    {
        // Encrypt a given stub using a XOR to break basic tools

        static string desc = "TODO";
        private static string Name = "StringEncryption";

        public LOEncrypt(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }

        // TODO: create a hash table for each function name
        //       create a custom hash function to funcname -> hash
        //       have another custom func handle the hash calling

        public override void Obfuscate()
        {
            throw new NotImplementedException();
        }

        public override string GetName()
        {
            return Name;
        }
    }
}
