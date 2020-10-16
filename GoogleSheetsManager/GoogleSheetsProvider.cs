using System;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace GoogleSheetsManager
{
    internal sealed class GoogleSheetsProvider : IDisposable
    {
        public GoogleSheetsProvider(string credentialJson, string sheetId)
        {
            GoogleCredential credential = GoogleCredential.FromJson(credentialJson).CreateScoped(Scopes);

            var initializer = new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            };

            _service = new SheetsService(initializer);
            _sheetId = sheetId;
        }

        public void Dispose() { _service.Dispose(); }

        public IEnumerable<IList<object>> GetValues(string range, bool parseValues = false)
        {
            SpreadsheetsResource.ValuesResource.GetRequest request = _service.Spreadsheets.Values.Get(_sheetId, range);
            request.ValueRenderOption = parseValues
                ? SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.UNFORMATTEDVALUE
                : SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMATTEDVALUE;
            request.DateTimeRenderOption =
                SpreadsheetsResource.ValuesResource.GetRequest.DateTimeRenderOptionEnum.SERIALNUMBER;
            ValueRange response = request.Execute();
            return response.Values;
        }

        private static readonly string[] Scopes = { SheetsService.Scope.Drive };
        private const string ApplicationName = "GoogleApisDriveProvider";

        private readonly SheetsService _service;
        private readonly string _sheetId;
    }
}
