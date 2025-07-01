#if ENABLE_SEARCH

#region sample

using System.Text;
using UnityEditor;
using UnityEditor.Localization.Search;
using UnityEditor.Search;
using UnityEngine;

public static class SearchSamples
{
    static void PrintResults(ISearchList results)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Found {results.Count} results:");
        foreach (var r in results)
        {
            // Extract the search data
            if (r.data is TableEntrySearchData tableData)
            {
                sb.AppendLine($"{tableData.Collection.TableCollectionName} - {tableData.Entry.Key}");
            }
        }
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Find all entries which contain the text "hello" in English.
    /// </summary>
    [MenuItem("Localization Samples/Search/Find Hello")]
    public static void FindHello()
    {
        var search = SearchService.Request("st: tr(en):hello", SearchFlags.Synchronous);
        PrintResults(search);
    }

    /// <summary>
    /// Find all entries which have an empty translated value
    /// </summary>
    [MenuItem("Localization Samples/Search/Find Empty")]
    public static void FindEmpty()
    {
        var search = SearchService.Request("st: tr=\"\"", SearchFlags.Synchronous);
        PrintResults(search);
    }

    /// <summary>
    /// Find all entries which have a reference to the MyFlag.png file
    /// </summary>
    [MenuItem("Localization Samples/Search/Find Png File")]
    public static void FindPngFile()
    {
        var search = SearchService.Request("at: tr=MyFlag.png", SearchFlags.Synchronous);
        PrintResults(search);
    }

    /// <summary>
    /// Find all entries which have an ItemGender metadata which contains a field or property called gender with the value Female.
    /// </summary>
    [MenuItem("Localization Samples/Search/Find Female Gender Items")]
    public static void FindFemaleGenderItems()
    {
        var search = SearchService.Request("st: mt=ItemGender mv(gender)=Female", SearchFlags.Synchronous);
        PrintResults(search);
    }
}

#endregion

#endif
