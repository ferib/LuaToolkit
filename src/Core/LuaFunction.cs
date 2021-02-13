using LuaSharpVM.Disassembler;
using LuaSharpVM.Models;
using System.Collections.Generic;

namespace LuaSharpVM.Core
{
    public class LuaFunction
    {
        public string Name;
        public int FirstLineNr;
        public int LastLineNr;
        public byte UpvaluesCount;
        public byte ArgsCount;
        public VarArg Vargs;
        public byte MaxStackSize;
        public List<LuaInstruction> Instructions;
        public List<LuaConstant> Constants;
        public List<LuaFunction> Functions;
        public List<int> DebugLines;
        public List<LuaLocal> DebugLocals;
        public List<string> DebugUpvalues;

        public LuaFunction()
        {
            // NOTE: remove?
            this.Instructions = new List<LuaInstruction>();
            this.Constants = new List<LuaConstant>();
            this.Functions = new List<LuaFunction>();
            this.DebugLines = new List<int>();
        }
    }
}
