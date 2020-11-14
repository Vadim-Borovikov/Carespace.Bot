using System.IO;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models.Save
{
    internal sealed class Manager
    {
        public Data Data { get; private set; }

        public Manager(string path)
        {
            _path = path;
            _locker = new object();
        }

        public void Save()
        {
            lock (_locker)
            {
                string json = JsonConvert.SerializeObject(Data, Formatting.Indented);
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
                    Data = JsonConvert.DeserializeObject<Data>(json);
                }
            }

            if (Data == null)
            {
                Data = new Data();
            }
        }

        private readonly string _path;
        private readonly object _locker;
    }
}