using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Core
{
	public class LuaLocal
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

		public LuaLocal(string name, int scopeStart, int scopeEnd)
		{
			Name = name;
			ScopeStart = scopeStart;
			ScopeEnd = scopeEnd;
		}
	}
}
