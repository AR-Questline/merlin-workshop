using Awaken.TG.Utility.Attributes.Tags;

namespace Awaken.TG.Main.Utility.Tags {
    public static class TagsEditorProxy {
        static RemoveTagDelegate s_removeTag;
        static RenameTagKindDelegate s_renameTagKind;
        static RenameTagValueDelegate s_renameTagValue;

        public static void SetDelegates(RemoveTagDelegate removeTag, RenameTagKindDelegate renameTagKind, RenameTagValueDelegate renameTagValue) {
            TagsEditorProxy.s_removeTag = removeTag;
            TagsEditorProxy.s_renameTagKind = renameTagKind;
            TagsEditorProxy.s_renameTagValue = renameTagValue;
        }
        
        public static void RemoveTag(string tag, TagsCategory category) {
            s_removeTag?.Invoke(tag, category);
        }
        
        public static void RenameTagKind(string oldKind, string newKind, TagsCategory category) {
            s_renameTagKind?.Invoke(oldKind, newKind, category);
        }
        
        public static void RenameTagValue(string oldTag, string newTag, TagsCategory category) {
            s_renameTagValue?.Invoke(oldTag, newTag, category);
        }
        
        public delegate void RemoveTagDelegate(string tag, TagsCategory category);
        public delegate void RenameTagKindDelegate(string oldKind, string newKind, TagsCategory category);
        public delegate void RenameTagValueDelegate(string oldTag, string newTag, TagsCategory category);
    }
}