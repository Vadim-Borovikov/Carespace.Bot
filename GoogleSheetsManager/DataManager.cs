using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleSheetsManager
{
    public sealed class DataManager : IDisposable
    {
        public DataManager(string credentialJson, string sheetId)
        {
            _provider = new GoogleSheetsProvider(credentialJson, sheetId);
        }

        public void Dispose() => _provider.Dispose();

        public IList<T> GetValues<T>(string range) where T : ILoadable, new()
        {
            IEnumerable<IList<object>> values = _provider.GetValues(range, true);
            return values?.Select(LoadValues<T>).ToList();
        }

        public static string ToString(IList<object> values, int index) => To(values, index, o => o?.ToString());
        public static DateTime? ToDateTime(IList<object> values, int index) => To(values, index, ToDateTime);
        public static TimeSpan? ToTimeSpan(IList<object> values, int index) => To(values, index, ToTimeSpan);
        public static Uri ToUri(IList<object> values, int index) => To(values, index, ToUri);
        public static int? ToInt(IList<object> values, int index) => To(values, index, ToInt);

        private static T LoadValues<T>(IList<object> values) where T : ILoadable, new()
        {
            var instance = new T();
            instance.Load(values);
            return instance;
        }

        private static T To<T>(IList<object> values, int index, Func<object, T> cast)
        {
            object o = values.Count > index ? values[index] : null;
            return cast(o);
        }

        private static DateTime? ToDateTime(object o)
        {
            switch (o)
            {
                case double d:
                    return DateTime.FromOADate(d);
                case long l:
                    return DateTime.FromOADate(l);
                default:
                    return null;
            }
        }

        private static TimeSpan? ToTimeSpan(object o) => ToDateTime(o)?.TimeOfDay;

        private static Uri ToUri(object o)
        {
            string uriString = o?.ToString();
            return string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString);
        }

        private static int? ToInt(object o) => int.TryParse(o?.ToString(), out int i) ? (int?) i : null;

        private readonly GoogleSheetsProvider _provider;
    }
}
