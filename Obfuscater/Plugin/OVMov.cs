using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class OVMov : LOPlugin
    {
        // Movfuscation, turning machiene
        // 'You sping my head right round right round'

        static string desc = "TODO";
        public OVMov(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }

        /*
        
        Constants =
        { 0, 1, .. }; // 0=0; 1=1

        LOADK 2 // cell_1
        LOADK 2 
        LOADK 3 // cell_2
        LOADK 3 
        LOADK 4 // cell_3
        LOADK 4
        MOVE var_x var_a (cell)
        MOVE var_y var_a
        LOADK 0 //0
        LOADK 1 //1


         */
    }
}
