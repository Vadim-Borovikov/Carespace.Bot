using System;
using AbstractBot;
using GryphonUtilities;

namespace Carespace.Bot;

public sealed class Config : ConfigGoogleSheets
{
    internal readonly string GoogleRange;
    internal readonly Uri EventsFormUri;
    internal readonly DateTime EventsUpdateAt;
    internal readonly string SavePath;
    internal readonly long EventsChannelId;

    internal long LogsChatId => SuperAdminId.GetValue(nameof(SuperAdminId));

    public Config(string token, string systemTimeZoneId, string dontUnderstandStickerFileId,
        string forbiddenStickerFileId, TimeSpan sendMessageDelayPrivate, TimeSpan sendMessageDelayGroup,
        TimeSpan sendMessageDelayGlobal, string googleCredentialJson, string applicationName, string googleSheetId,
        string googleRange, Uri eventsFormUri, DateTime eventsUpdateAt, string savePath, long eventsChannelId)
        : base(token, systemTimeZoneId, dontUnderstandStickerFileId, forbiddenStickerFileId, sendMessageDelayPrivate,
            sendMessageDelayGroup, sendMessageDelayGlobal, googleCredentialJson, applicationName, googleSheetId)
    {
        GoogleRange = googleRange;
        EventsFormUri = eventsFormUri;
        EventsUpdateAt = eventsUpdateAt;
        SavePath = savePath;
        EventsChannelId = eventsChannelId;
    }
}
