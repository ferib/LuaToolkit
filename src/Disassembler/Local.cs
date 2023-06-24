using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Disassembler
{
    public class Local
    {
        public string Name
        {
            get;
            set;
        }

        public int ScopeStart
        {
            get;
            set;
        }

        public int ScopeEnd
        {
            get;
            set;
        }

        public Local(string name, int scopeStart, int scopeEnd)
        {
            Name = name;
            // start pc
            ScopeStart = scopeStart;
            // end pc
            ScopeEnd = scopeEnd;
        }

        public string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Name).Append(": Start ").Append(ScopeStart)
                .Append(" End ").Append(ScopeEnd);

            return sb.ToString();
        }
    }
}
