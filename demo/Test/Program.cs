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

            LuaObfuscator o = new LuaObfuscator(File.ReadAllBytes(@"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\debuffspread.luac"));
            
            
            o.Obfuscate(null);


            Console.ReadLine();

            //var luaF = new LuaCFile(File.ReadAllBytes(@"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\RamerDouglasPeucker.luac"));
            var luaF = new LuaCFile(File.ReadAllBytes(@"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\debuffspread.luac"));
            LuaDecoder d = new LuaDecoder(luaF);
            LuaWriter writer = new LuaWriter(ref d);

            Console.WriteLine(writer.LuaScript);

            //var luaFile2 = new LuaCFile(File.ReadAllBytes("test.luac"));
            //LuaDecoder decoder2 = new LuaDecoder(luaFile2);

            //Console.WriteLine(decoder2.File.Function.Constants);

            //LuaEncoder encode = new LuaEncoder(luaFile2);
            //File.WriteAllBytes("test_out.luac", encode.SaveFile());

            //LuaObfuscator obfuscator = new LuaObfuscator(File.ReadAllBytes("test.luac"));

            //Console.WriteLine("Original Code");
            //Console.WriteLine("----------------------------------");
            //Console.ForegroundColor = ConsoleColor.DarkYellow;
            //Console.WriteLine(obfuscator.DecompileOriginalLuaC());
            //Console.ForegroundColor = ConsoleColor.Gray;

            //Console.WriteLine("Obuscated Code");
            //Console.WriteLine("----------------------------------");
            //Console.ForegroundColor = ConsoleColor.Green;
            //Console.WriteLine(obfuscator.DecompileObfuscatedLuaC());
            //Console.ForegroundColor = ConsoleColor.Gray;

            Console.ReadLine();
        }
    }
}
