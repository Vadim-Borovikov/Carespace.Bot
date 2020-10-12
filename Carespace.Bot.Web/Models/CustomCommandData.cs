using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GoogleDocumentsUnifier.Logic;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models
{
    internal class CustomCommandData
    {
        public Task Clear(ITelegramBotClient client, long chatId)
        {
            _pdfs.Clear();

            List<Task> tasks = _messageIds.Select(id => client.DeleteMessageAsync(chatId, id)).ToList();
            _messageIds.Clear();
            return Task.WhenAll(tasks);
        }

        public void AddMessage(Message message) => _messageIds.Add(message.MessageId);

        public void AddPdf(string name) => _pdfs.Add(name, 0);
        public void UpdatePdfAmount(string name, uint amount) => _pdfs[name] = amount;

        public IReadOnlyList<DocumentRequest> GetRequestedPdfs(string pdfFolderPath)
        {
            return _pdfs.Where(r => r.Value > 0).Select(r => CreateRequest(r.Key, r.Value, pdfFolderPath)).ToList();
        }

        private static DocumentRequest CreateRequest(string name, uint amount, string pdfFolderPath)
        {
            string path = Path.Combine(pdfFolderPath, $"{name}.pdf");
            return new DocumentRequest(path, amount);
        }

        private readonly List<int> _messageIds = new List<int>();
        private readonly Dictionary<string, uint> _pdfs = new Dictionary<string, uint>();
    }
}
