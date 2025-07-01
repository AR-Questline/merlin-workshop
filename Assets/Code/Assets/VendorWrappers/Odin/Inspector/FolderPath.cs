using System;

namespace Sirenix.OdinInspector
{
    public class FolderPathAttribute : Attribute
    {
        public bool AbsolutePath;
        public string ParentFolder;
        public bool RequireValidPath;
        public bool RequireExistingPath;
        public bool UseBackslashes;
    }
}