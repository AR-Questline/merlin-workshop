using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Interactions {
    /// <summary>
    /// Marker for interaction object colliders. Made to help in future changes in interaction detection.
    /// </summary>
    public class InteractionObject : MonoBehaviour {
        public const string InteractionTag = "InteractionObject";
        
        public enum ExtensionSide {
            [UnityEngine.Scripting.Preserve] All,
            [UnityEngine.Scripting.Preserve] Front,
            [UnityEngine.Scripting.Preserve] Back
        }

        public bool extendRaycast;

        [BoxGroup("0", false)]
        [ShowIf(nameof(extendRaycast))]
        [RichEnumExtends(typeof(ExtensionDistance))]
        [SerializeField]
        RichEnumReference _extensionDistance;

        [BoxGroup("0")]  
        [EnumToggleButtons]
        [ShowIf(nameof(extendRaycast))]
        public ExtensionSide side;

        public ExtensionDistance ExtensionDistance => _extensionDistance.EnumAs<ExtensionDistance>();

        public static void SetupForInteraction(InteractionObject interactionObject) {
            interactionObject.tag = InteractionTag;
            interactionObject.gameObject.layer = RenderLayers.PlayerInteractions;
        }
    }
}