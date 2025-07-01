using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

public static class TableCollectionSamples
{
    #region row-enumerator

    [MenuItem("CONTEXT/StringTableCollection/Print CSV")]
    public static void CreateCSV(MenuCommand command)
    {
        var collection = command.context as StringTableCollection;

        StringBuilder sb = new StringBuilder();

        // Header
        sb.Append("Key,");
        foreach (var table in collection.StringTables)
        {
            sb.Append(table.LocaleIdentifier);
            sb.Append(",");
        }
        sb.Append("\n");

        // Add each row
        foreach (var row in collection.GetRowEnumerator())
        {
            // Key column
            sb.Append(row.KeyEntry.Key);
            sb.Append(",");

            foreach (var tableEntry in row.TableEntries)
            {
                // The table entry will be null if no entry exists for this key
                sb.Append(tableEntry == null ? string.Empty : tableEntry.Value);
                sb.Append(",");
            }
            sb.Append("\n");
        }

        // Print the contents. You could save it to a file here.
        Debug.Log(sb.ToString());
    }

    #endregion
}
