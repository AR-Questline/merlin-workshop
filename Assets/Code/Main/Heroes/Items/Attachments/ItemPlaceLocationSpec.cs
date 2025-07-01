using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Rare, "For items that can place locations on ground (bonfire).")]
    public class ItemPlaceLocationSpec : MonoBehaviour, IAttachmentSpec {
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)})]
        ShareableARAssetReference locationPlaceholderValidPlacePrefab;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)})]
        ShareableARAssetReference locationPlaceholderInvalidPlacePrefab;
        [SerializeField, TemplateType(typeof(LocationTemplate))]
        TemplateReference location;
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)})]
        ShareableARAssetReference placementVfx;
        [SerializeField] float maximumPlacementDistance = 7f;
        [SerializeField] float minimumPlacementDistance = 1.5f;
        [SerializeField] bool consumeOnPlacement;
        [SerializeField] bool onlyOnMainScene = true;
        [FormerlySerializedAs("onlyInWyrdness")] [SerializeField] bool outsideRepellersOnly;
        [SerializeField] bool onlyInFactionlessTerrain;
        [SerializeField] bool interactAfterPlaced;
        [SerializeField] bool canBePlacedInWater;
        [SerializeField, HideIf(nameof(canBePlacedInWater))] LayerMask waterLayerMask;
        
        [Header("Optional: Required materials for use")]
        [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference requiredMaterialForUse;
        [SerializeField, ShowIf(nameof(RequiresMaterial))] int requiredMaterialForUseCount = 1;

        [Header("Minimum distance from specific locations")]
        [SerializeField]
        bool requireMinimumDistanceFromCookingStations = true;
        [SerializeField, ShowIf(nameof(requireMinimumDistanceFromCookingStations))]
        float minDistanceFromBlockingLocations = 10f;
        
        public ARAssetReference LocationPlaceholderValidPlacePrefab => locationPlaceholderValidPlacePrefab.Get();
        public ARAssetReference LocationPlaceholderInvalidPlacePrefab => locationPlaceholderInvalidPlacePrefab.Get();
        public LocationTemplate LocationTemplate => location.Get<LocationTemplate>(this);
        public ShareableARAssetReference PlacementVfx => placementVfx;
        
        public ItemTemplate RequiredMaterialsForUse => requiredMaterialForUse.TryGet<ItemTemplate>(this);
        public int RequiredMaterialForUseCount => requiredMaterialForUseCount;
        public bool RequireMinimumDistanceFromCookingStations => requireMinimumDistanceFromCookingStations;
        public float MinDistanceFromBlockingLocations => minDistanceFromBlockingLocations;

        public float MaximumPlacementDistance => maximumPlacementDistance;
        public float MinimumPlacementDistance => minimumPlacementDistance;
        public bool ConsumeOnPlacement => consumeOnPlacement;
        public bool OnlyOnMainScene => onlyOnMainScene;
        public bool OutsideRepellersOnly => outsideRepellersOnly;
        public bool OnlyInFactionlessTerrain => onlyInFactionlessTerrain;
        public bool CanBePlacedInWater => canBePlacedInWater;
        public LayerMask WaterLayerMask => waterLayerMask;
        public bool InteractAfterPlaced => interactAfterPlaced;
        bool RequiresMaterial => requiredMaterialForUse != null && requiredMaterialForUse.IsSet;

        public Element SpawnElement() {
            return new ItemPlaceLocation();
        }

        public bool IsMine(Element element) {
            return element is ItemPlaceLocation;
        }
    }
}