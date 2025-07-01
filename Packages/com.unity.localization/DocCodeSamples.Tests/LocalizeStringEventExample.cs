using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class LocalizeStringEventExample : MonoBehaviour
{
    [Serializable]
    public class CharacterData
    {
        public string name;
        public string surname;
        public int age;
    }

    public CharacterData characterInfo = new CharacterData { name = "Guy", surname = "Threepwood", age = 17 };

    public LocalizeStringEvent localizedStringEvent;
    LocalizedString originalLocalizedString;

    // Various ways to address a string table entry
    public LocalizedString localizedString = new LocalizedString { TableReference = "My String Table Collection", TableEntryReference = "My Text 1" };
    public string tableName = "My String Table Collection";
    public string keyName = "My Text 2";
    public long keyId = 123;

    void Start()
    {
        // Keep track of the original so we dont change localizedString by mistake
        originalLocalizedString = localizedStringEvent.StringReference;

        // Assuming the Localizing String is using Smart Format:
        // The string
        // "{name} {surname} is {age} years old"
        // would produce:
        // "Guy Threepwood is 17 years old"
        localizedStringEvent.StringReference.Arguments = new[] { characterInfo };

        // We can add a listener if we are interested in the Localized String.
        localizedStringEvent.OnUpdateString.AddListener(OnStringChanged);
    }

    void OnStringChanged(string s)
    {
        Debug.Log($"String changed to `{s}`");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Change using LocalizedString"))
        {
            // We are assigning a new LocalizedString so will need to copy the arguments across
            localizedString.Arguments = localizedStringEvent.StringReference.Arguments;

            // Assign the new LocalizedString, this will trigger an update
            localizedStringEvent.StringReference = localizedString;
        }

        if (GUILayout.Button("Change using key name"))
        {
            // Restore the original LocalizedString in case we changed it previously.
            localizedStringEvent.StringReference = originalLocalizedString;

            // Assign a new Table and Entry. This will trigger an update.
            localizedStringEvent.StringReference.SetReference(tableName, keyName);

            // We could do this if we only wanted to change the entry but use the same table
            // localizedStringEvent.StringReference.TableEntryReference = keyName;
        }

        if (GUILayout.Button("Change using key id"))
        {
            // Restore the original LocalizedString in case we changed it previously.
            localizedStringEvent.StringReference = originalLocalizedString;

            // Assign a new Table and Entry. This will trigger an update.
            localizedStringEvent.StringReference.SetReference(tableName, keyId);

            // We could do this if we only wanted to change the entry but use the same table
            // localizedStringEvent.StringReference.TableEntryReference = keyId;
        }

        if (GUILayout.Button("Increase Age"))
        {
            characterInfo.age++;

            // Force the string to refresh
            localizedStringEvent.RefreshString();
        }

        if (GUILayout.Button("Decrease Age"))
        {
            characterInfo.age--;

            // Force the string to refresh
            localizedStringEvent.RefreshString();
        }
    }
}
