using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG._3DAssets.AssetManager.Templates
{
    [CreateAssetMenu(fileName = "VFX_", menuName = "Asset Manager/VFX Data")] [UnityEngine.Scripting.Preserve]
    public class VFXTemplate : ScriptableObject {
        [TabGroup("Info"), LabelWidth(80)] [UnityEngine.Scripting.Preserve]
        public new string name = "";
        [TabGroup("Info"), LabelWidth(80), TextArea] [UnityEngine.Scripting.Preserve]
        public string discription;
        [TabGroup("Info"), LabelWidth(80), TextArea] [UnityEngine.Scripting.Preserve]
        public string feedback;
        [TabGroup("Components"), Required] [UnityEngine.Scripting.Preserve]
        public GameObject prefab;
        [TabGroup("Components"), InlineEditor(InlineEditorModes.SmallPreview, Expanded = true), Required] [UnityEngine.Scripting.Preserve]
        public List<AudioClip> sfx;
        [TabGroup("Setup"), LabelWidth(80)] [UnityEngine.Scripting.Preserve]
        public float timing = 1f;
        [TabGroup("Setup"), LabelWidth(80)] [UnityEngine.Scripting.Preserve]
        public Color color = new Color(1,0,0,1);
        [TabGroup("Setup"), LabelWidth(80)] [UnityEngine.Scripting.Preserve]
        public float size = 1f;
    }
}