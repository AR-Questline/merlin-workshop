using Awaken.TG.Editor.Utility.RichEnumReference;
using Awaken.TG.Editor.VisualScripting.Utils;
using Awaken.TG.Main.Utility.RichEnums;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using RichEnumRef = Awaken.TG.Main.Utility.RichEnums.RichEnumReference;

namespace Awaken.TG.Editor.VisualScripting.Properties {
    [UsedImplicitly, Inspector(typeof(RichEnumReference))]
    public class RichEnumReferenceInspector : Inspector {
        MetadataAccessor<string> EnumRef => new(this, "_enumRef");

        public RichEnumReferenceInspector(Metadata metadata) : base(metadata) {
            metadata.value ??= new RichEnumReference("");
        }

        public override float GetAdaptiveWidth() {
            return Mathf.Max(10, EditorStyles.popup.CalcSize(new GUIContent(RichEnumReferencePropertyDrawer.DisplayNameFromRef(EnumRef.Get()))).x + 1);
        }

        protected override float GetHeight(float width, GUIContent label) {
            return EditorStyles.popup.CalcHeight(label, width) + 2;
        }

        protected override void OnGUI(Rect position, GUIContent label) {
            string enumRef = EnumRef.Get();
            string search = string.Empty;
            var filter = metadata.GetAttribute<RichEnumExtendsAttribute>();
            
            RichEnumReferencePropertyDrawer.DrawSelectionControl(position, label, enumRef, ref search, filter,
                richEnum => EnumRef.Set(RichEnumRef.GetEnumRef(richEnum)));
        }
    }
}