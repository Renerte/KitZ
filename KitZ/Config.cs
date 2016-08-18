using System.IO;
using Newtonsoft.Json;

namespace KitZ
{
    public class Config
    {
        public string MySqlHost = "";
        public string MySqlDbName = "";
        public string MySqlUsername = "";
        public string MySqlPassword = "";

        public string ReloadSuccess = "Reloaded successfully!";

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Config Read(string path)
        {
            return File.Exists(path) ? JsonConvert.DeserializeObject<Config>(File.ReadAllText(path)) : new Config();
        }
    }
}
