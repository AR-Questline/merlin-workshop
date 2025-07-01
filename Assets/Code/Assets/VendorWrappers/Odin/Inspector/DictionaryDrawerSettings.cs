using System;

namespace Sirenix.OdinInspector
{
    public class DictionaryDrawerSettings : Attribute
    {
        public string KeyLabel = "Key";
        public string ValueLabel = "Value";
        public DictionaryDisplayOptions DisplayMode;
        public bool IsReadOnly;
        public float KeyColumnWidth = 130f;
    }
}