using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG._3DAssets.AssetManager.Templates
{
    [CreateAssetMenu(fileName = "Asset_", menuName = "Asset Manager/Asset Data")] [UnityEngine.Scripting.Preserve]
    public class AssetTemplate : ScriptableObject{
        
        [TabGroup("Info"), LabelWidth(80)] [UnityEngine.Scripting.Preserve]
        public new string name;
        
        [TabGroup("Info"), LabelWidth(80), TextArea] [UnityEngine.Scripting.Preserve]
        public string discription;
        
        [TabGroup("Info"), LabelWidth(80), TextArea] [UnityEngine.Scripting.Preserve]
        public string feedback;

        [TabGroup("Prefab"), InlineEditor(InlineEditorModes.LargePreview, Expanded = true), Required] [UnityEngine.Scripting.Preserve]
        public GameObject prefab;

        [TabGroup("Materials"), InlineEditor(InlineEditorModes.GUIAndHeader, Expanded = true)] [UnityEngine.Scripting.Preserve]
        public List<Material> materials;
        
        [TabGroup("VFX")] [UnityEngine.Scripting.Preserve]
        public List<GameObject> vfx;
        
        [TabGroup("SFX"), InlineEditor(InlineEditorModes.SmallPreview, Expanded = true)] [UnityEngine.Scripting.Preserve]
        public List<AudioClip> sfx;
        
        
    }
}
