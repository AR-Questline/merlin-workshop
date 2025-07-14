using System;
using Animancer;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Animations.FightingStyles;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Awaken.TG.Assets {
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class ARAssetReferenceSettingsAttribute : PropertyAttribute {
        
        public Type[] AssetTypes { get; }
        public AddressableGroup Group { get; }
        public string[] Labels { get; }
        public string GroupName => Group.NameOf();
        public Func<Object, string> NameProvider { get; }

        public ARAssetReferenceSettingsAttribute(Type[] assetTypes, bool useNameProvider = false, AddressableGroup group = AddressableGroup.Default, string[] labels = null) {
            AssetTypes = assetTypes;
            if (useNameProvider) {
#if UNITY_EDITOR
                NameProvider = static o => UnityEditor.AssetDatabase.LoadMainAssetAtPath(UnityEditor.AssetDatabase.GetAssetPath(o)).name;
#else
                NameProvider = static o => o.name;
#endif
            }
            
            Group = group;
            Labels = labels ?? Array.Empty<string>();
        }
    }

    public class UIAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public UIAssetReferenceAttribute(params string[] labels) : this(AddressableGroup.UI, labels) { }
        public UIAssetReferenceAttribute(AddressableGroup group, params string[] labels) : base(new[] {typeof(Sprite), typeof(Texture2D)}, true, group, labels) { }
    }
    
    public class PresenterAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        [UnityEngine.Scripting.Preserve]
        public PresenterAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(VisualTreeAsset), typeof(StyleSheet)}, true, AddressableGroup.UIPresenters, labels) { }
        public PresenterAssetReferenceAttribute(Type[] types, params string[] labels) : base(types, true, AddressableGroup.UIPresenters, labels) { }
    }

    public class PrefabAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public PrefabAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(GameObject)}, true, AddressableGroup.Default, labels) { }
        public PrefabAssetReferenceAttribute(AddressableGroup group, params string[] labels) : base(new[] {typeof(GameObject)}, true, group, labels) { }
    }
    
    public class MeshAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public MeshAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(Mesh)}, true, AddressableGroup.Default, labels) { }
        [UnityEngine.Scripting.Preserve] 
        public MeshAssetReferenceAttribute(AddressableGroup group, params string[] labels) : base(new[] {typeof(Mesh)}, true, group, labels) { }
    }
    
    public class AnimationClipAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public AnimationClipAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(AnimationClip)}, true, AddressableGroup.Animations, labels) { }
    }
    
    public class ClipTransitionAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public ClipTransitionAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(ClipTransitionAsset)}, true, AddressableGroup.AnimatorOverrides, labels) { }
    }
    
    public class AnimancerAnimationsAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public AnimancerAnimationsAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(ARStateToAnimationMapping)}, true, AddressableGroup.AnimatorOverrides, labels) { }
    }
    
    public class HeroAnimancerAnimationsAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public HeroAnimancerAnimationsAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(ARHeroStateToAnimationMapping)}, true, AddressableGroup.AnimatorOverrides, labels) { }
    }
    
    public class HeroAnimancerBaseAnimationsReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public HeroAnimancerBaseAnimationsReferenceAttribute(params string[] labels) : base(new[] {typeof(ARHeroAnimancerBaseAnimations)}, true, AddressableGroup.AnimatorOverrides, labels) { }
    }
    
    [UnityEngine.Scripting.Preserve]
    public class AnimancerConditionalAnimationsAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public AnimancerConditionalAnimationsAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(ARConditionalStateToAnimationMapping)}, true, AddressableGroup.AnimatorOverrides, labels) { }
    }
    
    public class EnemyBehavioursAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public EnemyBehavioursAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(AREnemyBehavioursMapping)}, true, AddressableGroup.EnemyBehaviours, labels) { }
    }
    
    public class TextureAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public TextureAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(Texture2D)}, true, AddressableGroup.NPCs, labels) { }
    }
    
    public class MaterialAssetReferenceAttribute : ARAssetReferenceSettingsAttribute {
        public MaterialAssetReferenceAttribute(params string[] labels) : base(new[] {typeof(Material)}, true, AddressableGroup.NPCs, labels) { }
    }
}