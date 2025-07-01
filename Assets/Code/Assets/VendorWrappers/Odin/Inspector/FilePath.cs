using System;

namespace Sirenix.OdinInspector
{
    public class FilePathAttribute : Attribute
    {
        public bool AbsolutePath;
        public string Extensions;
        public string ParentFolder;
        public bool RequireValidPath;
        public bool RequireExistingPath;
        public bool UseBackslashes;
        public bool IncludeFileExtension = true;
        public bool ReadOnly;
    }
}