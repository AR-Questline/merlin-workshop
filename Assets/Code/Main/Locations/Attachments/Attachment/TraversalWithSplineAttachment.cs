using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.MVC.Elements;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Splines;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [RequireComponent(typeof(SplineContainer))]
    public class TraversalWithSplineAttachment : MonoBehaviour, IAttachmentSpec {
        public float acceleration = 5f;
        public float maxMoveSpeed = 50f;
        public SplineContainer Spline => GetComponent<SplineContainer>();
        [PrefabAssetReference]
        public ARAssetReference fastTravelVisual;
        [Title("VFX"), ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        public ShareableARAssetReference spawnVfx;
        [ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        public ShareableARAssetReference onPathEndVFX;
        [ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.VFX)]
        public ShareableARAssetReference discardVfx;
        [Title("SFX")] public EventReference traversalSFX;
        
        public Element SpawnElement() {
            return new TraversalWithSpline();
        }
        
        public bool IsMine(Element element) {
            return element is TraversalWithSpline;
        }
    }
}