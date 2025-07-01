using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Localization.Plugins.CSV.Columns
{
    /// <summary>
    /// Provides preconfigured columns mappings that can be used with <see cref="Csv"/>.
    /// </summary>
    public static class ColumnMapping
    {
        /// <summary>
        /// Creates default mappings which include <see cref="KeyIdColumns"/> and a <see cref="LocaleColumns"/> for each project locale.
        /// </summary>
        /// <param name="includeComments">Should a column be added for comments extracted from metadata?</param>
        /// <returns>The list of column mappings.</returns>
        public static List<CsvColumns> CreateDefaultMapping(bool includeComments = true)
        {
            var columns = new List<CsvColumns>();

            columns.Add(new KeyIdColumns { IncludeSharedComments = includeComments });
            AddLocaleMappings(columns, includeComments);
            return columns;
        }

        /// <summary>
        /// Adds a <see cref="LocaleColumns"/> for any that are missing, using the project locales as the source.
        /// </summary>
        /// <param name="cells">The current list of <see cref="CsvColumns"/>.</param>
        /// <param name="includeComments">Should the new <see cref="LocaleColumns"/> include comments extracted from metadata?</param>
        public static void AddLocaleMappings(IList<CsvColumns> cells, bool includeComments = true)
        {
            var projectLocales = LocalizationEditorSettings.GetLocales();
            foreach (var locale in projectLocales)
            {
                // The locale is already mapped so we can ignore it
                if (cells.Any(c => c is LocaleColumns lc && lc.LocaleIdentifier == locale.Identifier))
                    continue;

                cells.Add(new LocaleColumns { LocaleIdentifier = locale.Identifier, IncludeComments = includeComments });
            }
        }
    }
}
