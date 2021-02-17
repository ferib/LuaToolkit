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
        private LOSettings Settings;
        private byte[] ObfuscatedLuaC;

        public LuaObfuscator(byte[] originalLuaC)
        {
            this.ObfuscatedLuaC = originalLuaC;
            this.Decoder = new LuaDecoder(new LuaCFile(this.ObfuscatedLuaC));
            this.Decompiler = new LuaWriter(ref this.Decoder);
        }

        public string Obfuscate(string settings)
        {
            this.Settings = new LOSettings(ref this.Decoder, settings);
            this.Settings.Execute();

            // add watermark
            this.Decoder.File.Function.Constants.Add(new StringConstant("cromulon.io"));
            return this.Decompiler.LuaScript;
        }
    }
}
