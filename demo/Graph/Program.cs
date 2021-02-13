using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using LuaSharpVM.Core;
using LuaSharpVM.Decompiler;
using LuaSharpVM.Disassembler;

namespace Graph
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //var luaF = new LuaCFile(File.ReadAllBytes(@"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\debuffspread.luac"));
            var luaF = new LuaCFile(File.ReadAllBytes(@"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\RamerDouglasPeucker.luac"));
            LuaDecoder d = new LuaDecoder(luaF);
            LuaWriter writer = new LuaWriter(ref d);

            Application.Run(new frmGraph(writer));
        }
    }
}
