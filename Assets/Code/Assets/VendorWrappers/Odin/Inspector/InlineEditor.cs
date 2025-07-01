using System;

namespace Sirenix.OdinInspector
{
    public class InlineEditorAttribute : Attribute
    {
        public bool DrawHeader;
        public bool DrawGUI;
        public bool DrawPreview;
        public float MaxHeight;
        public float PreviewWidth = 100f;
        public float PreviewHeight = 35f;
        public bool IncrementInlineEditorDrawerDepth = true;
        public InlineEditorObjectFieldModes ObjectFieldMode;
        public bool DisableGUIForVCSLockedAssets = true;
        public PreviewAlignment PreviewAlignment = PreviewAlignment.Right;

        public bool Expanded { get => default; set { } }

        public bool ExpandedHasValue { get => default; set { } }

        public InlineEditorAttribute(InlineEditorModes inlineEditorMode = InlineEditorModes.GUIOnly, InlineEditorObjectFieldModes objectFieldMode = InlineEditorObjectFieldModes.Boxed) { }
        public InlineEditorAttribute(InlineEditorObjectFieldModes objectFieldMode) { }
    }
    
    public enum InlineEditorObjectFieldModes
    {
        Boxed,
        Foldout,
        Hidden,
        CompletelyHidden,
    }
    
    public enum InlineEditorModes
    {
        GUIOnly,
        GUIAndHeader,
        GUIAndPreview,
        SmallPreview,
        LargePreview,
        FullEditor,
    }
    
    public enum PreviewAlignment
    {
        Left,
        Right,
        Top,
        Bottom,
    }
}