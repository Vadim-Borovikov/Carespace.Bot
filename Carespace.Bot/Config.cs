using System;
using AbstractBot;

namespace Carespace.Bot;

public sealed class Config : ConfigGoogleSheets
{
    public readonly string GoogleRange;
    public readonly Uri EventsFormUri;
    public readonly DateTime EventsUpdateAt;
    public readonly string SavePath;
    internal readonly long EventsChannelId;

    internal long? LogsChatId => SuperAdminId;

    public Config(string token, string systemTimeZoneId, string dontUnderstandStickerFileId,
        string forbiddenStickerFileId, TimeSpan sendMessageDelay, string googleCredentialJson, string applicationName,
        string googleSheetId, string googleRange, Uri eventsFormUri, DateTime eventsUpdateAt, string savePath,
        long eventsChannelId)
        : base(token, systemTimeZoneId, dontUnderstandStickerFileId, forbiddenStickerFileId, sendMessageDelay,
            googleCredentialJson, applicationName, googleSheetId)
    {
        GoogleRange = googleRange;
        EventsFormUri = eventsFormUri;
        EventsUpdateAt = eventsUpdateAt;
        SavePath = savePath;
        EventsChannelId = eventsChannelId;
    }
}
