using System.Collections.Generic;

namespace GoogleSheetsReader
{
    public interface ILoadable
    {
        void Load(IList<object> values, int index);
    }
}