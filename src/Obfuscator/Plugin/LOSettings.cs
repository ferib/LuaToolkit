using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LuaSharpVM.Decompiler;
using LuaSharpVM.Disassembler;
using Newtonsoft.Json;

namespace LuaSharpVM.Obfuscator.Plugin
{
    public class LOSettingCollection
    {
        public string FunctionName;
        public List<LOPlugin> Plugins;
        //public List<int> Settings;
    }

    public class LOSettings
    {
        private List<LOSettingCollection> ActivePlugins;
        private LuaDecoder Decoder;

        public LOSettings()
        {
            this.ActivePlugins = new List<LOSettingCollection>();
        }

        public LOSettings(ref LuaDecoder decoder, string settings)
        {
            this.Decoder = decoder;
            this.ActivePlugins = new List<LOSettingCollection>();
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
            
        }

        public bool Execute()
        {
            // executes the obfuscation on the current decoder
            if (this.Decoder == null || this.ActivePlugins == null || this.ActivePlugins.Count == 0)
                return false;

            var list = this.ActivePlugins.ToList();
            foreach (var sc in list)
                for(int i = 0; i < sc.Plugins.Count; i++)
                    sc.Plugins[i].Obfuscate(sc.Plugins[i].Level); // NOTE: go like this, ye?

            return true;
        }

        public string[] GetListedFunctionNames()
        {
            var keyList = this.ActivePlugins.ToList();
            string[] res = new string[keyList.Count];
            for (int i = 0; i < res.Length; i++)
                res[i] = keyList[i].FunctionName;
            return res;
        }

        public void AddPlugin<T>(string functionName, LOPlugin plugin)
        {
            var target = this.ActivePlugins.Find(x => x.FunctionName == functionName);
            if (target != null)
            {
                var oldIndex = -1;
                var oldPlugin = target.Plugins.Find(x => x is T);
                if(oldPlugin != null)
                    target.Plugins.IndexOf(oldPlugin);

                if (oldIndex != -1)
                    target.Plugins.RemoveAt(oldIndex);
                target.Plugins.Add(plugin);

            } else
                this.ActivePlugins.Add(new LOSettingCollection()
                {
                    FunctionName = functionName,
                    Plugins = new List<LOPlugin>() { plugin }
                });
        }
    }
}
