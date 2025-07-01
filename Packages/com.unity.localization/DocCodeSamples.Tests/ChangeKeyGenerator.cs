using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

public class ChangeKeyGeneratorExample
{
    public void ChangeKeyGenerator()
    {
        var stringTableCollection = LocalizationEditorSettings.GetStringTableCollection("My Game Text");

        // Determine the highest Key Id so Unity can continue generating Ids that do not conflict with existing Ids.
        long maxKeyId = 0;
        if (stringTableCollection.SharedData.Entries.Count > 0)
            maxKeyId = stringTableCollection.SharedData.Entries.Max(e => e.Id);

        stringTableCollection.SharedData.KeyGenerator = new SequentialIDGenerator(maxKeyId + 1);

        // Mark the asset dirty so that Unity saves the changes
        EditorUtility.SetDirty(stringTableCollection.SharedData);
    }
}
