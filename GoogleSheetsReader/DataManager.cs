﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleSheetsReader
{
    public class DataManager : IDisposable
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
        public static Uri ToUri(IList<object> values, int index) => To(values, index, ToUri);

        private static T LoadValues<T>(IList<object> values, int index) where T : ILoadable, new()
        {
            var instance = new T();
            instance.Load(values, index);
            return instance;
        }

        private static T To<T>(IList<object> values, int index, Func<object, T> cast)
        {
            object o = values.Count > index ? values[index] : null;
            return cast(o);
        }

        private static DateTime? ToDateTime(object o) => o is double d ? (DateTime?) DateTime.FromOADate(d) : null;

        private static Uri ToUri(object o)
        {
            string uriString = o?.ToString();
            return string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString);
        }

        private readonly GoogleSheetsProvider _provider;
    }
}