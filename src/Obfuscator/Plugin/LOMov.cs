using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;

namespace LuaToolkit.Obfuscator.Plugin
{
    public class LOMov : LOPlugin
    {
        // Movfuscation, turning machiene
        // 'You sping my head right round right round'

        static string desc = "Implements the movfuscator turning machiene.";
        private static string Name = "Movfuscator";

        public LOMov(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }

        public override void Obfuscate()
        {
            throw new NotImplementedException();
        }

        public override string GetName()
        {
            return Name;
        }

        // Lua bool result generation:
        //
        // 01: LOADNIL  0 1
        // 02: LE       1 1 0   ; if false then GOTO 4
        // 03: JMP      0 1     ; to 5
        // 04: LOADBOOL 2 0 1   ; bool_2 = 1 (PC += 1)
        // 05: LOADBOOL 2 1 0   ; bool_2 = 0
        // 06: RETURN   2 2     ; return bool_2
        // 07: RETURN   0 1     ; end

        // Obfuscated bool:
        //
        // 01: 
        // 02: .. idk .. 
        // 03: 
        // 04: 
        // 05: 
        // 06: 
        // 07: 
        // 08: 
        // 09: 

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
