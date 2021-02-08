using System;
using System.Collections.Generic;
using System.Text;

namespace LuaSharpVM.Core
{
	public class LuaLocal
	{
		public string Name
		{
			get;
			private set;
		}

		public int ScopeStart
		{
			get;
			private set;
		}

		public int ScopeEnd
		{
			get;
			private set;
		}

		public LuaLocal(string name, int scopeStart, int scopeEnd)
		{
			Name = name;
			ScopeStart = scopeStart;
			ScopeEnd = scopeEnd;
		}
	}
}
