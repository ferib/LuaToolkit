using System;
using LuaSharpVM;
using LuaSharpVM.Decompiler;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Obfuscator;
using LuaSharpVM.Obfuscator.Plugin;
using System.IO;
using System.Collections.Generic;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[+] LuaSharpVM\r\n");
            //LuaObfuscator o = new LuaObfuscator(File.ReadAllBytes(@"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\test_if.luac"));
            //LuaObfuscator o = new LuaObfuscator(File.ReadAllBytes(@"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\frost.luac"));
            LuaObfuscator o = new LuaObfuscator(File.ReadAllBytes(@"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\testif.luac"));

            // show original lua
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            LuaWriter w = new LuaWriter(ref o.Decoder);
            Console.WriteLine(w.LuaScript);

            // obfuscate
            o.Obfuscate("{'test':123}"); // TODO

            // show obfuscated lua
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(new string('=', Console.WindowWidth));
            w = new LuaWriter(ref o.Decoder);
            Console.WriteLine(w.LuaScript);

            Console.ReadLine();
        }
    }
}
