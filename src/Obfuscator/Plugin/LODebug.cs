using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscator.Plugin
{
    public enum LODebugLevel
    {
        None = 0,
        RandomLow,  // minimal random
        RandomMedium, // more randomness to make it harder but may reveal the nonse
        EraseAll, // play it safe and have it erase!
    }
    public class LODebug : LOPlugin
    {
        // Add custom instruction by replacing pairs of existing ones
        static string desc = "Randomizing/removing debug information.";
        // NOTE: Have a semi-trusted debugging information may give the reverser 
        //       a harder time compared to giving no debugging information at all.

        private LODebugLevel Level;

        public LODebug(ref LuaDecoder decoder, LODebugLevel level) : base(ref decoder, desc)
        {
            this.Level = level;
            Obfuscate();
        }

        private void Obfuscate()
        {
            Console.WriteLine(desc);
            switch(this.Level)
            {
                case LODebugLevel.RandomLow:
                    CounterAllDebugLines();
                    RandomizeAllDebugLocals();
                    break;
                case LODebugLevel.RandomMedium:
                    CounterAllDebugLines();
                    RandomizeAllDebugLocals();
                    EraseAllDebugUpvalues();
                    break;
                case LODebugLevel.EraseAll:
                    EraseAllDebuginfo();
                    break;
            }
            
        }

        private void EraseAllDebuginfo()
        {
            base.Decoder.File.Function.DebugLines.Clear();
            base.Decoder.File.Function.DebugLocals.Clear();
            base.Decoder.File.Function.DebugUpvalues.Clear();
            foreach (var f in base.Decoder.File.Function.Functions)
            {
                f.DebugLines.Clear();
                f.DebugLocals.Clear();
                f.DebugUpvalues.Clear();
            }
        }
        private void CounterAllDebugLines()
        {
            CounterDebugLines(base.Decoder.File.Function);
            foreach (var f in base.Decoder.File.Function.Functions)
                CounterDebugLines(f);
        }

        private void CounterDebugLines(LuaFunction func)
        {
            Random rnd = new Random();
            int trigger = 24;
            if (this.Level == LODebugLevel.RandomMedium)
                trigger = 60;

            for (int i = 0; i < base.Decoder.File.Function.DebugLines.Count; i++)
                if(rnd.Next(0,100) > trigger)
                    base.Decoder.File.Function.DebugLines[i] += 1;
        }

        private void EraseAllDebugUpvalues()
        {
            EraseDebugUpvalues(base.Decoder.File.Function);
            foreach (var f in base.Decoder.File.Function.Functions)
                EraseDebugUpvalues(f);
        }
        
        private void EraseDebugUpvalues(LuaFunction func)
        {
            for (int i = 0; i < base.Decoder.File.Function.DebugUpvalues.Count; i++)
                    base.Decoder.File.Function.DebugUpvalues[i] = "";
        }

        private void RandomizeAllDebugLocals()
        {
            RandomizeDebugLocals(base.Decoder.File.Function);
            foreach (var f in base.Decoder.File.Function.Functions)
                RandomizeDebugLocals(f);
        }

        private void RandomizeDebugLocals(LuaFunction func)
        {
            Random rnd = new Random();
            List<string> rndPrefix = new List<string> { "is", "Get", "Set"}; // random prefix

            // collect randomPrefixes from existing locals
            for (int i = 0; i < func.DebugLocals.Count; i++)
            {
                // iterate to find upercase or underscore to indicate when strings end
                int start = 0;
                for(int j = 1; j < func.DebugLocals[i].Name.Length; j++)
                {
                    if(char.IsUpper(func.DebugLocals[i].Name[j]) && char.IsLower(func.DebugLocals[i].Name[j-1])
                        || func.DebugLocals[i].Name[j] == '_')
                    {
                        // split it!
                        string newWord = func.DebugLocals[i].Name.Substring(start, j - start);
                        if(rndPrefix.FindIndex(x => x.ToUpper() == newWord.ToUpper()) == -1)
                            rndPrefix.Add(newWord);
                        start = j+1;
                    }
                }
            }

            for (int i = 0; i < func.DebugLocals.Count; i++)
            {
                string fakeLocal = "";
                int count = rnd.Next(2, 5);
                for(int j = 0; j < count; j++)
                    fakeLocal += rndPrefix[rnd.Next(0, rndPrefix.Count)];
                func.DebugLocals[i].Name = fakeLocal;

                int offset = 2; // not to much, return instructions are obviously to see when missaligned
                if (this.Level == LODebugLevel.RandomMedium)
                    offset += 11; // lets go a little crazy here for the skids

                //////lets make sure they still align, people that reverse arent retarded afterall; No need to align, maybe I am retarded ?
                ////if (i != 0)
                ////    func.DebugLocals[i].ScopeStart = func.DebugLocals[i - 1].ScopeEnd;
                //if (i - 1 != func.DebugLocals.Count)
                //    func.DebugLocals[i].ScopeEnd += rnd.Next(0, offset); // stecht it!
                ////// NOTE: think it doesnt do much? watch https://www.decompiler.com/ break when you upload!
            }
        }
    }
}
