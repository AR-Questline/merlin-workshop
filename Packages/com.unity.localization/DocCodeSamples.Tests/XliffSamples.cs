using System.IO;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.XLIFF;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

public static class XliffSamples
{
    public static void WriteXliff()
    {
        #region create-xliff

        var document = XliffDocument.Create(XliffVersion.V20);

        // The lnaguage to translate from.
        document.SourceLanguage = "en";

        // The language to translate into.
        document.TargetLanguage = "fr";

        var file = document.AddNewFile();
        file.Id = "file id";

        var group = file.AddNewGroup();

        var start = group.AddNewTranslationUnit();
        start.Id = "12345";
        start.Name = "START";
        start.Source = "Start Game";

        var quit = group.AddNewTranslationUnit();
        quit.Id = "12346";
        quit.Name = "QUIT";
        quit.Source = "Quit Game";

        using (var stream = new FileStream("Exported XLIFF.xliff", FileMode.Create, FileAccess.Write))
        {
            document.Serialize(stream);
        }
        #endregion
    }

    #region load-xliff
    public static void LoadXliffFile(string file)
    {
        var document = XliffDocument.Parse(new FileStream(file, FileMode.Open, FileAccess.Read));
        if (document.Version == "1.2")
        {
            var v12 = document as UnityEditor.Localization.Plugins.XLIFF.V12.xliff;
            // Unity now has access to a 1.2 XLIFF document and can use 1.2 features.
            // The V12 namespace contains all the 1.2-specific nodes.
        }
        else
        {
            var v20 = document as UnityEditor.Localization.Plugins.XLIFF.V20.xliff;
            // Unity now has access to a 2.0 XLIFF document and can use 2.0 features.
            // The V20 namespace contains all the 2.0-specific nodes.
        }
    }

    #endregion

    #region export-xliff
    public static void ExportTable(StringTable source, StringTable target, string dir)
    {
        Xliff.Export(source, dir, XliffVersion.V20, new[] { target });
    }

    public static void ExportCollection(StringTableCollection collection, LocaleIdentifier sourceLanguage, string dir)
    {
        Xliff.Export(sourceLanguage, dir, "My XLIFF Export", XliffVersion.V12, new[] { collection });
    }

    public static void ImportIntoStringTable(StringTable table, string file)
    {
        Xliff.ImportFileIntoTable(file, table);
    }

    public static void ImportIntoCollection(StringTableCollection collection, string file)
    {
        Xliff.ImportFileIntoCollection(collection, file);
    }

    #endregion
}
