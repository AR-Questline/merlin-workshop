using UnityEngine.Localization;

namespace UnityEditor.Localization.UI
{
    [CustomPropertyDrawer(typeof(LocalizedStringTable), true)]
    class LocalizedStringTablePropertyDrawer : LocalizedTablePropertyDrawer<StringTableCollection>
    {
        static LocalizedStringTablePropertyDrawer()
        {
            GetProjectTableCollections = LocalizationEditorSettings.GetStringTableCollections;
        }
    }
}
