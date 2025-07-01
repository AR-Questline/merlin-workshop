using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public class LocalizedStringTableExample : MonoBehaviour
{
    public LocalizedStringTable m_StringTable = new LocalizedStringTable { TableReference = "My Strings" };

    string m_TranslatedStringHello;
    string m_TranslatedStringGoodbye;
    string m_TranslatedStringThisIsATest;

    void OnEnable()
    {
        m_StringTable.TableChanged += LoadStrings;
    }

    void OnDisable()
    {
        m_StringTable.TableChanged -= LoadStrings;
    }

    void LoadStrings(StringTable stringTable)
    {
        m_TranslatedStringHello = GetLocalizedString(stringTable, "Hello");
        m_TranslatedStringGoodbye = GetLocalizedString(stringTable, "Goodbye");
        m_TranslatedStringThisIsATest = GetLocalizedString(stringTable, "This is a test");
    }

    static string GetLocalizedString(StringTable table, string entryName)
    {
        var entry = table.GetEntry(entryName);

        // We can also extract Metadata here
        var comment = entry.GetMetadata<Comment>();
        if (comment != null)
        {
            Debug.Log($"Found metadata comment for {entryName} - {comment.CommentText}");
        }

        return entry.GetLocalizedString(); // We can pass in optional arguments for Smart Format or String.Format here.
    }

    void OnGUI()
    {
        if (!LocalizationSettings.InitializationOperation.IsDone)
        {
            GUILayout.Label("Initializing Localization");
            return;
        }

        GUILayout.Label(m_TranslatedStringThisIsATest);
        GUILayout.Label(m_TranslatedStringHello);
        GUILayout.Label(m_TranslatedStringGoodbye);
    }
}
