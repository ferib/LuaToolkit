using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Disassembler.Passes
{
    public abstract class BaseInstructionPass
    {
        public abstract bool RunOnFunction(Function function);

        // Function to run before running the pass.
        virtual public void InitPass() { }

        // Function to run after the pass is done.
        virtual public void FinalizePass() { }
    }
}
