using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LuaSharpVM.Decompiler;
using LuaSharpVM.Disassembler;
using Newtonsoft.Json;

namespace LuaSharpVM.Obfuscator.Plugin
{
    public class LOSettings
    {
        private List<LOPlugin> ActivePlugins;
        private LuaDecoder Decoder;

        public LOSettings()
        {
            this.ActivePlugins = new List<LOPlugin>();
        }

        public LOSettings(ref LuaDecoder decoder, string settings)
        {
            this.Decoder = decoder;
            this.ActivePlugins = new List<LOPlugin>();
            DeserialiseSettings(settings);
        }

        private void DeserialiseSettings(string settings)
        {
            // TODO: turn dynamic objects into LOSettingCollection
            dynamic ObjSettings = JsonConvert.DeserializeObject<object>(settings);
            //var test = ObjSettings.Value["test"];
            //for(int i = 0; i < ObjSettings.length; i++)
            //{
            //    this.AddPlugin<LODebug>(ObjSettings[i], new LODebug(ref this.Decoder));
            //}

            // NOTE: test demo
            //AddSetting<LODebug>("Exports", 2);
            //AddSetting<LOString>("Exports", 2);
            //AddSetting<LOString>("anotherRandom", 2);
            AddSetting<LOFlow>("unknown0", 2);
            //AddSetting<LOCompress>("unknown0", 2);
        }

        public bool Execute()
        {
            // executes the obfuscation on the current decoder
            if (this.Decoder == null || this.ActivePlugins == null || this.ActivePlugins.Count == 0)
                return false;

            foreach (var p in this.ActivePlugins)
                p.Obfuscate();

            return true;
        }

        public void AddSetting<T>(string functionName, int level)
        {
            var target = this.ActivePlugins.Find(x => x is T);
            if (target == null)
                this.ActivePlugins.Add((LOPlugin)Activator.CreateInstance(typeof(T), new object[] { this.Decoder }));

            target = this.ActivePlugins.Find(x => x is T);
            if(target != null)
            {
                target.Functions.Add(functionName);
                target.Levels.Add(level);
            }
                
        }
    }
}
