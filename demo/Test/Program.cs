using System;
using LuaToolkit;
using LuaToolkit.Decompiler;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;
using LuaToolkit.Obfuscator;
using LuaToolkit.Obfuscator.Plugin;
using LuaToolkit.Beautifier;
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
            LuaObfuscator o = new LuaObfuscator(File.ReadAllBytes(@"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\upvalues.luac"));

            // show original lua
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            LuaDecompiler w = new LuaDecompiler(o.Decoder);
            Console.WriteLine(w.LuaScript);

            // obfuscate
            o.Obfuscate("{'test':123}"); // TODO

            // show obfuscated lua
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(new string('=', Console.WindowWidth));
            w = new LuaDecompiler(o.Decoder);
            Console.WriteLine(LuaBeautifier.BeautifieScript(w.LuaScript));

            Console.ReadLine();
        }
    }
}
