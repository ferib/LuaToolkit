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

        public List<string> FullFileName;
        public List<string> FileName;
        public string Owner;
        public string Hash;

        public Compiler(string owner)
        {
            this.Owner = owner;
            this.FullFileName = new List<string>();
            this.FileName = new List<string>();
        }

        public void AddFiles(List<byte[]> Buffers, List<string> fileNames)
        {
            for (int i = 0; i < Buffers.Count; i++)
                AddFile(Buffers[i], fileNames[i]);
        }

        public void AddFile(byte[] Buffer, string fileName)
        {
            this.FileName.Add(fileName);
            fileName = this.Owner + "__" + fileName + "__" + DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss");
            this.FullFileName.Add(fileName);
            File.WriteAllBytes($"{Directory.GetCurrentDirectory()}/{CompiledInPath}{fileName}", Buffer);
            // TODO: hash = hash(Owner:Date:FileName)
        }

        public bool Compile()
        {
            if (!File.Exists(LuacPath))
                return false;

            Process p = new Process();
            //p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = LuacPath;
            p.StartInfo.Arguments = $"-o {CompiledOutPath}{this.Owner} ";
            for (int i = 0; i < this.FullFileName.Count; i++)
                p.StartInfo.Arguments += $"{CompiledInPath}{this.FullFileName[i]} ";
            p.Start();
            p.WaitForExit(); // timeout this?

            //if (!File.Exists(CompiledOutPath + this.FullFileName)) // fix check?
            //    return false;

            return true;
        }

        public byte[] GetCompiled()
        {
            if (File.Exists(CompiledOutPath + this.Owner))
                return File.ReadAllBytes(CompiledOutPath + this.Owner);

            return null;
        }
    }
}
