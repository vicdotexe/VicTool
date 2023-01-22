using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VicTool.Main.Misc
{
    public static class FileHelper
    {
        public static bool DeserializeJsonFile<T>(string path,out T jsonObj) where T : new()
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                jsonObj = JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            else
            {
                jsonObj = new T();
                return false;
            }
           
        }

        public static void SerializeToJsonFile(this object self, string path)
        {
            var json = JsonConvert.SerializeObject(self, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}
