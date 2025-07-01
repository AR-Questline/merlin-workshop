using System;

namespace Sirenix.OdinInspector
{
    public class AssetListAttribute : Attribute
    {
        public bool AutoPopulate;
        public string Tags;
        public string LayerNames;
        public string AssetNamePrefix;
        public string Path;
        public string CustomFilterMethod;
    }
}