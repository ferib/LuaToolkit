using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Decompiler;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscator.Plugin
{
    class LOCompress : LOPlugin
    {
        static string desc = "Compresses and packs the Lua ASCII script with basic XOR encryption";
        private static string Name = "Compresion";

        private static byte[] XorInstructions = new byte[]{ 00, 00};
        private static byte[] XorConstants = new byte[]{ 00, 00};
        private LuaFunction UnpackFunc;

        public LOCompress(ref LuaDecoder decoder) : base(ref decoder, desc)
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
            string funcDecode = GenerateRandomVariable(ref usedVars); // decodes characters
            string funcToTable = GenerateRandomVariable(ref usedVars); // decodes the LPH! table
            string funcLPHTable = GenerateRandomVariable(ref usedVars); // converts the LPH! to table


            string sectionOne = $"local {tablename} = {{}}\nfor {index} = 0, 255 do local {localA} = string.char(index) " +
                $"local {localB} = string.chat(index, 0) {tablename}[{localB}] = {localA} end";
            string sectionTwo = $"\nlocal {funcDecode} = function({localC}, {localE}, {localF}, {localD}) if {localF} >= 256 then {localF}, {localD} = 0, {localD} + 1 " +
                $"if {localD} >= 256 then {localE} = {{}} {localD} = 1 end end {localE}[string.char({localF}, {localD})] = {localC} {localF} = {localF} + 1" +
                $"return {localE}, {localF}, {localD} end";
            string sectionThree = $"\n-- TODO\n";
            string sectionFour = $"local {funcLPHTable} = function({localC}) return ({{{localC}:sub(5):gsub(\"..\", function(n) return string.char(tonumber(n, 16)) end)}})[1] end";
            string sectionFive = $"\nif not pcall(loadstring, \"return\") then error(\"Your Lua environment does not support loadstring, therefore you are unable to use the Luraph VM compression feature.\") end";
            string sectionSix = $"\nloadstring({funcToTable}({funcLPHTable}(\"LPH!{compressedScriptBytes}\")))()";

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

        public override void Obfuscate()
        {
            Console.WriteLine(CreateScript("REEEEEEEEEEEEEEEEEEEEEEEE"));
            //this.UnpackFunc = CreateUnpackFunction();

            //// add XOR decoding function
            //this.Decoder.File.Function.Functions.Add(this.UnpackFunc);
        }

        public override string GetName()
        {
            return Name;
        }
    }
}
