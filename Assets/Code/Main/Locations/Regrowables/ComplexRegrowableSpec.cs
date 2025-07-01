using Awaken.TG.Assets;
using Awaken.TG.Graphics.Previews;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.Main.Utility.Availability;
using Awaken.Utility.Maths.Data;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Regrowables {
    public class ComplexRegrowableSpec : SceneSpec, IRegrowableSpec, IInteractableWithHeroProvider, IAssetReferenceToPreview {
        [Title("Regrow")]
        [field: SerializeField, PrefabAssetReference(AddressableGroup.Locations, "Regrowable")]
        public ARAssetReference RegrowablePart { get; private set; }
        [field: SerializeField]
        public ARTimeSpan RegrowRate { get; private set; }

        [Title("Pick up")]
        [field: SerializeField] public ItemSpawningData ItemReference { get; private set; }
        [SerializeField, TemplateType(typeof(CrimeOwnerTemplate))] TemplateReference owner;
        [SerializeField] DayNightAvailability availability;

        [SerializeField] StoryBookmark storyOnPickedUp;
        
        Regrowable _regrowable;

        public IInteractableWithHero InteractableWithHero => _regrowable;
        public CrimeOwnerTemplate CrimeOwner(uint localId) => owner is { IsSet: true } ? owner.Get<CrimeOwnerTemplate>() : null;

        public uint Count => 1;
        public StoryBookmark StoryOnPickedUp => storyOnPickedUp;

        void Awake() {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#endif
            {
                availability.Init(Spawn, Despawn);
            }
        }

        void OnEnable() {
#if UNITY_EDITOR
            EDITOR_OnEnable();
            if (EditorApplication.isPlaying)
#endif
            {
                availability.Enable();
            }
        }

        void OnDisable() {
#if UNITY_EDITOR
            EDITOR_OnDisable();
            if (EditorApplication.isPlaying)
#endif
            {
                availability.Disable();
            }
        }

        void OnDestroy() {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
#endif
            {
                availability.Deinit();
            }
        }

        // === IRegrowableSpec
        public SpecId MVCId(uint localId) {
            return SceneId;
        }

        public SmallTransform Transform(uint localId) {
            var specTransform = transform;
            specTransform.GetPositionAndRotation(out var position, out var rotation);
            var localToWorld = new SmallTransform(position, rotation, specTransform.localScale);
            return localToWorld;
        }

        public string RegrowablePartKey(uint localId) {
            return RegrowablePart.RuntimeKey;
        }

        ItemSpawningData IRegrowableSpec.ItemReference(uint localId) {
            return ItemReference;
        }

        ARTimeSpan IRegrowableSpec.RegrowRate(uint localId) {
            return RegrowRate;
        }

        // === Main regrowable logic
        void Spawn() {
            _regrowable = new Regrowable(0, this);
            CullingSystemRegistrator.Register(_regrowable);
            RegrowableInitialization.Initialize(_regrowable);
        }

        void Despawn() {
            CullingSystemRegistrator.Unregister(_regrowable);
            RegrowableInitialization.Uninitialize(_regrowable);
            _regrowable = null;
        }

        // === IAssetReferenceToPreview
        public GameObject PreviewParent => gameObject;
        public ARAssetReference ToPreviewReference => RegrowablePart;
        public string DisablePreviewKey => "disableRegrowablePreviews";

#if UNITY_EDITOR
        void EDITOR_OnEnable() => ((IWithRenderersToPreview)this).RegisterToPreview();
        void EDITOR_OnDisable() => ((IWithRenderersToPreview)this).UnregisterFromPreview();
#endif
    }
}
