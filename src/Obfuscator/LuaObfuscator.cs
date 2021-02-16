using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LuaSharpVM.Core;
using LuaSharpVM.Models;
using LuaSharpVM.Decompiler;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Obfuscator.Plugin;

namespace LuaSharpVM.Obfuscator
{
    public class LuaObfuscator
    {
        public LuaDecoder Decoder;
        private LuaWriter Decompiler;
        private byte[] ObfuscatedLuaC;

        public LuaObfuscator(byte[] originalLuaC)
        {
            this.ObfuscatedLuaC = originalLuaC;
            this.Decoder = new LuaDecoder(new LuaCFile(this.ObfuscatedLuaC));
            this.Decompiler = new LuaWriter(ref this.Decoder);
        }

        public string Obfuscate(LOSettings settings)
        {

            //var plugins = settings;
            settings = new LOSettings(ref this.Decoder, "{'test':123}");
            
            // add watermark
            this.Decoder.File.Function.Constants.Add(new StringConstant("cromulon.io"));
            return this.Decompiler.LuaScript;
        }
    }
}
