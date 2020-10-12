namespace Carespace.Bot.Web.Models
{
    internal class PdfData
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

        public static PdfData CreateNoneLocal(string sourceId, string path)
        {
            return new PdfData(FileStatus.None, sourceId, path);
        }

        public static PdfData CreateOutdatedLocal(string sourceId, string path)
        {
            return new PdfData(FileStatus.Outdated, sourceId, path);
        }

        public static PdfData CreateNoneGoogle(string path)
        {
            return new PdfData(FileStatus.None, null, path);
        }

        public static PdfData CreateOutdatedGoogle(string path, string id)
        {
            return new PdfData(FileStatus.Outdated, null, path, id);
        }

        public static PdfData CreateOk() => new PdfData(FileStatus.Ok);

        private PdfData(FileStatus status, string sourceId = null, string path = null, string id = null)
        {
            Status = status;

            SourceId = sourceId;
            Path = path;
            Id = id;
        }
    }
}