using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GryphonUtilities.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Carespace.Bot;

internal static class PhotoRepository
{
    public static async Task<Message> SendPhotoAsync(Bot bot, Chat chat, string photoPath, string? caption = null,
        ParseMode? parseMode = null, IReplyMarkup? replyMarkup = null)
    {
        bool success = PhotoIds.TryGetValue(photoPath, out string? fileId);
        if (success && !string.IsNullOrWhiteSpace(fileId))
        {
            InputFileId photo = new(fileId);
            return await bot.SendPhotoAsync(chat, photo, replyMarkup, null, caption, parseMode);
        }

        await using (FileStream stream = new(photoPath, FileMode.Open))
        {
            InputFileStream photo = InputFile.FromStream(stream);
            Message message = await bot.SendPhotoAsync(chat, photo, replyMarkup, null, caption, parseMode);
            PhotoSize[] photoSizes = message.Photo.GetValue(nameof(message.Photo));
            fileId = photoSizes.First().FileId;
            PhotoIds.TryAdd(photoPath, fileId);
            return message;
        }
    }

    private static readonly ConcurrentDictionary<string, string> PhotoIds = new();
}