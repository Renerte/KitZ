using System.IO;
using Newtonsoft.Json;

namespace KitZ
{
    public class Config
    {
        public string ItemAddedToKit = "Added {0} to kit {1}";
        public string ItemRemovedFromKit = "Removed {0} from kit {1}";
        public string KitAdded = "Kit {0} was added.";
        public string KitGiven = "You used kit {0}.";
        public string KitNoPerm = "You don't have permission to use kit {0}!";
        public string KitNotFound = "Could not find kit {0}!";
        public string KitRemoved = "Removed kit {0}";
        public string MySqlDbName = "";
        public string MySqlHost = "";
        public string MySqlPassword = "";
        public string MySqlUsername = "";
        public string NoKitEntered = "Please enter kit name: /kit name";

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