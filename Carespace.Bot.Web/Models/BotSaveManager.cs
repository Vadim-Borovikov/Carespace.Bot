using System.IO;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models
{
    internal sealed class BotSaveManager
    {
        public BotSave Data { get; private set; }

        public BotSaveManager(string path)
        {
            _path = path;
            _locker = new object();
        }

        public void Save()
        {
            lock (_locker)
            {
                string json = JsonConvert.SerializeObject(Data);
                File.WriteAllText(_path, json);
            }
        }

        public void Load()
        {
            lock (_locker)
            {
                if (File.Exists(_path))
                {
                    string json = File.ReadAllText(_path);
                    Data = JsonConvert.DeserializeObject<BotSave>(json);
                }
            }

            if (Data == null)
            {
                Data = new BotSave();
            }
        }

        private readonly string _path;
        private readonly object _locker;
    }
}