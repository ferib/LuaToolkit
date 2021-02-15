using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Recompiler
{
    public class Compiler
    {
        public static string LuacPath = "lua/luac5.1";     // NOTE: make sure the LuaC 5.1 binary is installed right there
        public static string CompiledInPath = "lua/in/";    // stores all the Lua files that got uploaded
        public static string CompiledOutPath = "lua/out/";  // stores all the Lua files that got compiled

        public string FullFileName;
        public string FileName;
        public string Owner;
        public string Hash;

        public Compiler(byte[] Buffer, string fileName, string owner)
        {
            this.FileName = fileName;
            this.Owner = owner;
            this.FullFileName = this.Owner + "__" + this.FileName + "__" + DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");
            File.WriteAllBytes($"{Directory.GetCurrentDirectory()}/{CompiledInPath}{this.FullFileName}", Buffer);
            // TODO: hash = hash(Owner:Date:FileName)

            if (!Compile())
                Console.WriteLine("Compiling failed.."); 
        }

        private bool Compile()
        {
            if (!File.Exists(LuacPath))
                return false;

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = LuacPath;
            p.StartInfo.Arguments = $"-o {CompiledOutPath}{this.FullFileName} {CompiledInPath}{this.FullFileName}";
            p.Start();
            p.WaitForExit(); // timeout this?

            if (!File.Exists(CompiledOutPath + this.FullFileName))
                return false;

            return true;
        }

        public byte[] GetCompiled()
        {
            if (File.Exists(CompiledOutPath + this.FullFileName))
                return File.ReadAllBytes(CompiledOutPath + this.FullFileName);

            return null;
        }
    }
}
