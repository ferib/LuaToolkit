using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class LOProxyCall : LOPlugin
    {
        static string desc = "Adds proxy function that will try to find destination function by calculating crc32 checksum.";

        public LOProxyCall(ref LuaDecoder decoder) : base (ref decoder, desc)
        {
            // NOTE: Add proxy func with multi args
            //
            // var0 = _G['proxy_call']
            // var0("0344FE91", ...) -- calls "0344FE91" with args ...
            // proxy_call will then get list of names (somehow) and calculate crc32 checksums
            // until it has a match, once there is a match, it calls the input of the checksum
            // NOTE: this heavly reduces performance, only use on non performance critical stuff
        }
    }
}
