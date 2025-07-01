using CsvHelper;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Plugins.CSV.Columns
{
    /// <summary>
    /// Represents a CSV column that is responsible for identifying what entry the row should be mapped to.
    /// </summary>
    public interface IKeyColumn
    {
        /// <summary>
        /// Returns the entry that should be mapped for the current row or <see langword="null"/> if one can not be found.
        /// </summary>
        /// <param name="reader">Provides read access to the CSV file.</param>
        /// <returns>The mapped entry or <see langword="null"/> if one could not be found.</returns>
        SharedTableData.SharedTableEntry ReadKey(CsvReader reader);
    }
}
