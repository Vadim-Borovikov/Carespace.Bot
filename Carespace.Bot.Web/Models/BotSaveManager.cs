using System.IO;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models
{
    internal sealed class BotSaveManager
    {
        public BotSave Data { get; private set; }

        public BotSaveManager(string path) { _path = path; }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(Data);
            File.WriteAllText(_path, json);
        }

        public void Load()
        {
            if (File.Exists(_path))
            {
                string json = File.ReadAllText(_path);
                Data = JsonConvert.DeserializeObject<BotSave>(json);
            }

            if (Data == null)
            {
                Reset();
            }
        }

        public void Reset() => Data = new BotSave();

        private readonly string _path;
    }
}