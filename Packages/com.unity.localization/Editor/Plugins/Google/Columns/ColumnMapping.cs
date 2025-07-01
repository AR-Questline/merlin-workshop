using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization;

namespace UnityEditor.Localization.Plugins.Google.Columns
{
    /// <summary>
    /// Provides preconfigured columns mappings that can be used with <see cref="GoogleSheets"/>.
    /// </summary>
    public static class ColumnMapping
    {
        /// <summary>
        /// Returns the next available column name. For example if <paramref name="columns"/> was using "A", "B", "D" then "C" would be returned.
        /// </summary>
        /// <param name="columns">The columns that are currently in use.</param>
        /// <returns>The next available column name.</returns>
        public static string GetNextAvailableColumn(IList<SheetColumn> columns) => GetNextAvailableColumn(new HashSet<int>(columns.Select(c => c.ColumnIndex)));

        /// <summary>
        /// Returns the next available column name. For example if <paramref name="reservedColumns"/> was "A", "B", "D" then "C" would be returned.
        /// </summary>
        /// <param name="reservedColumns">The column names that are currently in use.</param>
        /// <returns>The next available column name.</returns>
        public static string GetNextAvailableColumn(params string[] reservedColumns) => GetNextAvailableColumn(new HashSet<int>(reservedColumns.Select(SheetColumn.ColumnNameToIndex)));

        /// <summary>
        /// Returns the next available sheet column.
        /// </summary>
        /// <param name="reservedColumIds">The reserved column ids where "A" = 0, "B" = 1 etc. The found available column will also be added.</param>
        /// <returns></returns>
        public static string GetNextAvailableColumn(HashSet<int> reservedColumIds)
        {
            int colIdx = 0;
            while (reservedColumIds.Contains(colIdx))
            {
                colIdx++;
            }
            reservedColumIds.Add(colIdx);
            return SheetColumn.IndexToColumnName(colIdx);
        }

        /// <summary>
        /// Creates a KeyColumn at "A" and then a <see cref="LocaleColumn"/> for each <see cref="Locale"/> in the project, each mapped to a unique Column.
        /// </summary>
        /// <returns></returns>
        public static List<SheetColumn> CreateDefaultMapping()
        {
            var columns = new List<SheetColumn>();
            columns.Add(new KeyColumn { Column = "A" });
            AddLocaleMappings(columns);
            return columns;
        }

        /// <summary>
        /// Creates a <see cref="LocaleColumn"/> for any project <see cref="Locale"/> that is missing from the columns.
        /// </summary>
        /// <param name="columns">The existing column that will also be appeneded with any missing <see cref="Locale"/>'s</param>
        public static void AddLocaleMappings(IList<SheetColumn> columns)
        {
            var usedColumnIds = new HashSet<int>(columns.Select(c => c.ColumnIndex));
            var projectLocales = LocalizationEditorSettings.GetLocales();

            foreach (var locale in projectLocales)
            {
                // The locale is already mapped so we can ignore it
                if (columns.Any(c => c is LocaleColumn lc && lc.LocaleIdentifier == locale.Identifier))
                    continue;

                columns.Add(new LocaleColumn { LocaleIdentifier = locale.Identifier, Column = GetNextAvailableColumn(usedColumnIds) });
            }
        }

        /// <summary>
        /// Creates columns by attempting to match to expected column names(case insensitive).<br/>
        /// The following names are checked:
        /// <list type="bullet">
        /// <item>
        /// <term>key</term>
        /// <description><see cref="KeyColumn"/></description>
        /// </item>
        /// <item>
        /// <term>Project <see cref="Locale"/>'s name, <see cref="LocaleIdentifier.ToString"/> or <see cref="LocaleIdentifier.Code"/></term>
        /// <description><see cref="LocaleColumn"/></description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="columNames">The column names to create mappings for.</param>
        /// <param name="unusedNames">Optional list that can be populated with the names that a match could not be found for.</param>
        /// <returns></returns>
        public static List<SheetColumn> CreateMappingsFromColumnNames(IList<string> columNames, IList<string> unusedNames = null)
        {
            var columns = new List<SheetColumn>();

            // We map all potential name variations into the dictionary and then check each name against it.
            // We could cache this however we would have to keep it in sync with the Locale's so for simplicity's sake, we don't.
            var nameMap = new Dictionary<string, Func<SheetColumn>>(StringComparer.OrdinalIgnoreCase);

            nameMap["Key"] = () => new KeyColumn();

            var projectLocales = LocalizationEditorSettings.GetLocales();
            foreach (var locale in projectLocales)
            {
                Func<SheetColumn> createLocaleFunc = () => new LocaleColumn { LocaleIdentifier = locale.Identifier };
                Func<SheetColumn> createCommentFunc = () => new LocaleCommentColumn { LocaleIdentifier = locale.Identifier };
                nameMap[locale.name] = createLocaleFunc;
                nameMap[locale.LocaleName] = createLocaleFunc;
                nameMap[locale.Identifier.ToString()] = createLocaleFunc;
                nameMap[locale.Identifier.Code] = createLocaleFunc;

                // Comment
                nameMap[locale.name + " Comments"] = createCommentFunc;
                nameMap[locale.LocaleName + " Comments"] = createCommentFunc;
                nameMap[locale.Identifier.ToString() + " Comments"] = createCommentFunc;
                nameMap[locale.Identifier.Code + " Comments"] = createCommentFunc;
            }

            // Now map the columns
            for (int i = 0; i < columNames.Count; ++i)
            {
                if (nameMap.TryGetValue(columNames[i], out var createFunc))
                {
                    var column = createFunc();
                    column.ColumnIndex = i;
                    columns.Add(column);
                }
                else if (unusedNames != null)
                {
                    unusedNames.Add(columNames[i]);
                }
            }

            return columns;
        }
    }
}
