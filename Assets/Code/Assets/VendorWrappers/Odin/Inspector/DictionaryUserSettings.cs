namespace Sirenix.OdinInspector
{
    public class DictionaryUserSettings
    {
        public string KeyLabel = "Key";
        public string ValueLabel = "Value";
        public DictionaryDisplayOptions DisplayMode;
        public bool IsReadOnly;
        public float KeyColumnWidth = 130f;
    }

    public enum DictionaryDisplayOptions
    {
        OneLine,
        Foldout,
        CollapsedFoldout,
        ExpandedFoldout,
    }
}