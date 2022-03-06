using System;
using System.Collections.Generic;
using System.Linq;
using GoogleSheetsManager;
using GryphonUtilities;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models;

public sealed class ConfigJson : IConvertibleTo<Config>
{
    [JsonProperty]
    public string? Token { get; set; }
    [JsonProperty]
    public string? SystemTimeZoneId { get; set; }
    [JsonProperty]
    public string? DontUnderstandStickerFileId { get; set; }
    [JsonProperty]
    public string? ForbiddenStickerFileId { get; set; }

    [JsonProperty]
    public string? Host { get; set; }
    [JsonProperty]
    public List<string?>? About { get; set; }
    [JsonProperty]
    public List<string?>? ExtraCommands { get; set; }
    [JsonProperty]
    public List<long?>? AdminIds { get; set; }
    [JsonProperty]
    public long? SuperAdminId { get; set; }

    [JsonProperty]
    public string? GoogleCredentialJson { get; set; }
    [JsonProperty]
    public string? ApplicationName { get; set; }
    [JsonProperty]
    public string? GoogleSheetId { get; set; }

    [JsonProperty]
    public string? GoogleRange { get; set; }
    [JsonProperty]
    public Uri? EventsFormUri { get; set; }
    [JsonProperty]
    public DateTime? EventsUpdateAt { get; set; }
    [JsonProperty]
    public string? SavePath { get; set; }
    [JsonProperty]
    public long? EventsChannelId { get; set; }

    [JsonProperty]
    public string? AdminIdsJson { get; set; }
    [JsonProperty]
    public string? CultureInfoName { get; set; }
    [JsonProperty]
    public Dictionary<string, string?>? GoogleCredential { get; set; }

    public Config Convert()
    {
        string token = Token.GetValue(nameof(Token));
        string systemTimeZoneId = SystemTimeZoneId.GetValue(nameof(SystemTimeZoneId));
        string dontUnderstandStickerFileId = DontUnderstandStickerFileId.GetValue(nameof(DontUnderstandStickerFileId));
        string forbiddenStickerFileId = ForbiddenStickerFileId.GetValue(nameof(ForbiddenStickerFileId));

        string googleCredentialJson = string.IsNullOrWhiteSpace(GoogleCredentialJson)
            ? JsonConvert.SerializeObject(GoogleCredential)
            : GoogleCredentialJson;
        string applicationName = ApplicationName.GetValue(nameof(ApplicationName));
        string googleSheetId = GoogleSheetId.GetValue(nameof(GoogleSheetId));

        string googleRange = GoogleRange.GetValue(nameof(GoogleRange));
        Uri eventsFormUri = EventsFormUri.GetValue(nameof(EventsFormUri));
        DateTime eventsUpdateAt = EventsUpdateAt.GetValue(nameof(EventsUpdateAt));
        string savePath = SavePath.GetValue(nameof(SavePath));

        long eventsChannelId = EventsChannelId.GetValue(nameof(EventsChannelId));

        if (AdminIds is null || (AdminIds.Count == 0))
        {
            string json = AdminIdsJson.GetValue(nameof(AdminIdsJson));
            AdminIds = JsonConvert.DeserializeObject<List<long?>>(json);
        }

        return new Config(token, systemTimeZoneId, dontUnderstandStickerFileId, forbiddenStickerFileId,
            googleCredentialJson, applicationName, googleSheetId, googleRange, eventsFormUri,
            eventsUpdateAt, savePath, eventsChannelId)
        {
            Host = Host,
            About = About is null ? null : string.Join(Environment.NewLine, About),
            ExtraCommands = ExtraCommands is null ? null : string.Join(Environment.NewLine, ExtraCommands),
            AdminIds = AdminIds?.Select(id => id.GetValue("Admin id")).ToList(),
            SuperAdminId = SuperAdminId,
        };
    }
}