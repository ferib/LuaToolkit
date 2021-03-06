using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit.Decompiler;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;

namespace LuaToolkit.Obfuscator.Plugin
{
    class LOPacker : LOPlugin
    {
        static string desc = "Packs the Lua script with fake Luraph packer";
        private static string Name = "Fake Luraph Packer";

        private static byte[] XorInstructions = new byte[]{ 00, 00};
        private static byte[] XorConstants = new byte[]{ 00, 00};
        private LuaFunction UnpackFunc;

        public LOPacker(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }

        private LuaFunction CreateUnpackFunction()
        {
            // For shit & giggles lets make it look like luraph xD



            return null;
        }

        private string CreateScript(string compressedScriptBytes)
        {
            List<string> usedVars = new List<string>();
            string tablename = GenerateRandomVariable(ref usedVars);
            string index = GenerateRandomVariable(ref usedVars);
            string localA = GenerateRandomVariable(ref usedVars); // upval
            string localB = GenerateRandomVariable(ref usedVars); // upval
            string localC = GenerateRandomVariable(ref usedVars); // funcLPHTable(localC), funcDecode 1
            string localD = GenerateRandomVariable(ref usedVars); // funcDecode 4
            string localE = GenerateRandomVariable(ref usedVars); // funcDecode 2
            string localF = GenerateRandomVariable(ref usedVars); // funcDecode 3
            string localG = GenerateRandomVariable(ref usedVars); // 
            string localH = GenerateRandomVariable(ref usedVars); // 
            string localI = GenerateRandomVariable(ref usedVars); // 
            string localJ = GenerateRandomVariable(ref usedVars); //  
            string localK = GenerateRandomVariable(ref usedVars); // 
            string localL = GenerateRandomVariable(ref usedVars); // 
            string localM = GenerateRandomVariable(ref usedVars); // 
            string localN = GenerateRandomVariable(ref usedVars); // 
            string localO = GenerateRandomVariable(ref usedVars); // 
            string funcDecode = GenerateRandomVariable(ref usedVars); // decodes characters
            string funcDecodeTable = GenerateRandomVariable(ref usedVars); // decodes the LPH! table
            string funcLPHTable = GenerateRandomVariable(ref usedVars); // converts the LPH! to table


            string sectionOne = $"local {tablename} = {{}}\nfor {index} = 0, 255 do local {localA} = string.char(index) " +
                $"local {localB} = string.chat(index, 0) {tablename}[{localB}] = {localA} end";
            string sectionTwo = $"\nlocal {funcDecode} = function({localC}, {localE}, {localF}, {localD}) if {localF} >= 256 then {localF}, {localD} = 0, {localD} + 1 " +
                $"if {localD} >= 256 then {localE} = {{}} {localD} = 1 end end {localE}[string.char({localF}, {localD})] = {localC} {localF} = {localF} + 1" +
                $"return {localE}, {localF}, {localD} end";
            // LocalC = byte_table

            // localL = arg_len
            // LocalF = table_map_chars
            // LocalJ = left_char
            // LocalI = right_char
            // LocalG = result
            // LocalH = current_byte
            // LocalE = table_index
            // LocalD = unk_str
            // LocalM = next_str
            // LocalN = current_str
            // LocalO = next_byte
            string sectionThree = $"\nlocal function {funcDecodeTable}({localC}) local {localL} = #{localC} local {localF} = {{}} local {localJ}, {localI} = 0, 1 " +
                $"local {localG} = {{}} local {localE} = 1 local {localH} = string.sub({localC}, 1, 2) {localG}[{localE}] = {tablename}[{localH}] or {localF}[{localH}] " +
                $"{localE} = {localE} + 1 for {index} = 3, {localL}, 2 do local {localO} = string.sub({localC}, {index}, {index} + 1) local {localN} = {tablename}[{localH}] or {localF}[{localH}] " +
                $"local {localM} = {tablename}[{localO}] or {localF}[{localO}] if {localM} then {localG}[{localE}] = {localM} {localE} = {localE} + 1 {localF}, {localJ}, {localI} = " +
                $"{funcDecode}({localN} .. string.sub({localM}, 1, 1), {localF}, {localJ}, {localI}) else local {localD} = {localN} .. string.sub({localN}, 1, 1) {localG}[{localE}] = {localD} " +
                $"{localE} = {localE} + 1 {localF}, {localJ}, {localI} = {funcDecode}({localD},{localF},{localJ},{localI}) end {localH} = {localO} end return table.concat({localG}) end";
            string sectionFour = $"\nlocal {funcLPHTable} = function({localC}) return ({{{localC}:sub(5):gsub(\"..\", function(iliIi) return string.char(tonumber(iliIi, 16)) end)}})[1] end";
            string sectionFive = $"\nif not pcall(loadstring, \"return\") then error(\"Your Lua environment does not support loadstring, therefore you are unable to use the Luraph VM compression feature.\") end";
            string sectionSix = $"\nloadstring({funcDecodeTable}({funcLPHTable}(\"LPH!{compressedScriptBytes}\")))()";

            return sectionOne + sectionTwo + sectionThree + sectionFour + sectionFive + sectionSix;
        }

        private string GenerateRandomVariable(ref List<string> blacklist)
        {
            string result = "";
            Random rnd = new Random();
            int len = rnd.Next(19,22); // 19 or 21 in Luraph scripts
            char[] il = { 'l', 'I', 'l', '1', 'i' };
            do
            {
                while (result.Length < len)
                {
                    char c = il[rnd.Next(0, il.Length)];
                    if (result.Length == 0 && c == '1')
                        continue; // do not start with num

                    result += c;

                }
            } while (blacklist.Contains(result)); // repear if already exist
            return result;
        }

        private string GeneratePackedString(string str)
        {
            // TODO: reverse the packer and find out how it works
            string result = "";
            foreach (var b in Encoding.UTF8.GetBytes(str))
                result += b.ToString("X2"); // TODO: use correct pack format
            return result;
        }

        public override void Obfuscate()
        {
            // TODO: dont just write it to the console?
            Console.WriteLine(CreateScript(GeneratePackedString(this.Decoder.File.Function.ScriptFunction.GetText())));
        }

        public override string GetName()
        {
            return Name;
        }
    }
}
