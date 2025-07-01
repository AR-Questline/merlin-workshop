using System;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

public class PlatformOverrideExamples
{
    #region table-override

    public void SetupTableOverrideInEditor()
    {
        // Get the 2 table collections. 1 for default and 1 for our chosen platform (PS4).
        var collection = LocalizationEditorSettings.GetStringTableCollection("My Strings");
        var collectionPs4 = LocalizationEditorSettings.GetStringTableCollection("My Strings PS4");

        var englishTable = collection.GetTable("en") as StringTable;
        var englishTablePs4 = collectionPs4.GetTable("en") as StringTable;

        // Add the default entry
        var entry = englishTable.AddEntry("COPYRIGHT_NOTICE", "This is some copyright info for general platforms...");

        // Add the entry we want to use on PS4 using the same entry name.
        englishTablePs4.AddEntry("COPYRIGHT_NOTICE", "This is some copyright info for PS4 platforms...");

        // Set up the platform override so that COPYRIGHT_NOTICE redirects to a different table but uses the same key.
        var platformOverride = new PlatformOverride();
        platformOverride.AddPlatformTableOverride(RuntimePlatform.PS4, "My Strings PS4");
        entry.SharedEntry.Metadata.AddMetadata(platformOverride);

        // Mark the assets dirty so changes are saved
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(englishTable);
    }

    #endregion

    #region entry-override

    public void SetupEntryOverrideInEditor()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection("My Strings");
        var englishTable = collection.GetTable("en") as StringTable;

        // Add the default entry
        var entry = englishTable.AddEntry("COPYRIGHT_NOTICE", "This is some copyright info for general platforms...");

        // Add the entry we want to use on PS4
        englishTable.AddEntry("COPYRIGHT_NOTICE_PS4", "This is some copyright info for PS4 platforms...");

        // Set up the platform override so that COPYRIGHT_NOTICE redirects to COPYRIGHT_NOTICE_PS4 when running on PS4.
        var platformOverride = new PlatformOverride();
        platformOverride.AddPlatformEntryOverride(RuntimePlatform.PS4, "COPYRIGHT_NOTICE_PS4");
        entry.SharedEntry.Metadata.AddMetadata(platformOverride);

        // Mark the assets dirty so changes are saved
        EditorUtility.SetDirty(collection.SharedData);
        EditorUtility.SetDirty(englishTable);
    }

    #endregion
}

#region custom-entry-override

[Serializable]
[Metadata(AllowedTypes = MetadataType.AllTableEntries | MetadataType.StringTableEntry)]
public class DayOverride : IEntryOverride
{
    public LocalizedString myOverride = new LocalizedString();

    public DayOfWeek day = DayOfWeek.Friday;

    public EntryOverrideType GetOverride(out TableReference tableReference, out TableEntryReference tableEntryReference)
    {
        if (DateTime.Now.DayOfWeek == day)
        {
            tableReference = myOverride.TableReference;
            tableEntryReference = myOverride.TableEntryReference;
            return EntryOverrideType.TableAndEntry;
        }

        // Do not override.
        tableReference = default;
        tableEntryReference = default;
        return EntryOverrideType.None;
    }
}

#endregion
