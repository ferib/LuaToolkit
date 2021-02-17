using System;
using System.IO;
using LuaSharpVM.Obfuscator;

namespace Recompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            // NOTE: Please download and install Lua 5.1 and link the LuaC binary to ./lua/luac
            Console.Write("Please enter the path of a file: ");
            string path = Console.ReadLine();

            if(!File.Exists(path))
            {
                Console.WriteLine("File doesnt exist");
                Console.ReadLine();
                return;
            }

            Compiler comp = new Compiler(File.ReadAllBytes(path), "Test.luac", "Ferib");
            Decompiler dec = new Decompiler(comp.GetCompiled());
            LuaObfuscator obfuscator = new LuaObfuscator(comp.GetCompiled());
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(dec.GetResult());
            Console.ForegroundColor = ConsoleColor.Cyan;
            //Console.WriteLine(obfuscator);
            Console.ReadLine();
        }
    }
}
