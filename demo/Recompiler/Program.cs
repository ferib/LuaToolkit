using System;
using System.IO;
using System.Collections.Generic;
using LuaSharpVM.Obfuscator;

namespace Recompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            // NOTE: Please download and install Lua 5.1 and link the LuaC binary to ./lua/luac
            Console.WriteLine("Please enter the path of your project files: ");
            //string path = Console.ReadLine();
            //string path = @"H:\games\World of Warcraft\_retail_\Interface\AddOns\dubai";
            string path = @"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\testAddon";

            if (!Directory.Exists(path))
            {
                Console.WriteLine("Directory doesn't exist");
                Console.ReadLine();
                return;
            }

            Compiler comp = new Compiler("Ferib");
            // create file list
            List<string> Paths = new List<string>();
            List<byte[]> Buffers = new List<byte[]>();

            discoverDirectory(path, ref Paths, ref Buffers);
            void discoverDirectory(string dir, ref List<string> paths, ref List<byte[]> buffers)
            {
                var files = Directory.GetFiles(dir);
                foreach(var f in files)
                {
                    if (!f.Contains(".lua"))
                        continue;
                    Buffers.Add(File.ReadAllBytes(f));
                    paths.Add(f.Split('\\')[f.Split('\\').Length-1]);
                }

                var dirs = Directory.GetDirectories(dir);
                foreach (var d in dirs)
                    discoverDirectory(d, ref paths, ref buffers);
            }
            comp.AddFiles(Buffers, Paths);
            if(!comp.Compile())
            {
                Console.WriteLine("Compiler error!!");
                Console.ReadKey();
                return;
            }

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
