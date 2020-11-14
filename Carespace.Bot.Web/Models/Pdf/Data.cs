namespace Carespace.Bot.Web.Models.Pdf
{
    internal sealed class Data
    {
        public enum FileStatus
        {
            None,
            Outdated,
            Ok
        }

        public readonly FileStatus Status;

        public readonly string SourceId;
        public readonly string Path;
        public readonly string Id;
        public string Name => System.IO.Path.GetFileName(Path);

        public static Data CreateNoneLocal(string sourceId, string path) => new Data(FileStatus.None, sourceId, path);

        public static Data CreateOutdatedLocal(string sourceId, string path)
        {
            return new Data(FileStatus.Outdated, sourceId, path);
        }

        public static Data CreateNoneGoogle(string path) => new Data(FileStatus.None, null, path);

        public static Data CreateOutdatedGoogle(string path, string id)
        {
            return new Data(FileStatus.Outdated, null, path, id);
        }

        public static Data CreateOk() => new Data(FileStatus.Ok);

        private Data(FileStatus status, string sourceId = null, string path = null, string id = null)
        {
            Status = status;

            SourceId = sourceId;
            Path = path;
            Id = id;
        }
    }
}