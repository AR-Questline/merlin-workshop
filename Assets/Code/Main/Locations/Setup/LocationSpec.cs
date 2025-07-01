using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates.Specs;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.GameObjects;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Pool;
#if UNITY_EDITOR
using UnityEditor;
using Awaken.TG.Main.Locations.Setup.Editor;
#endif

namespace Awaken.TG.Main.Locations.Setup {
    [ExecuteInEditMode, HideMonoScript]
    public class LocationSpec : BaseSpec, ITagged, IAttachmentGroup, IModelProvider, ISnappable, IDrakeRepresentationOptionsProvider {
        // === Basic data
        [FoldoutGroup("Design"), LocStringCategory(Category.Location)] public LocString displayName;

        [FoldoutGroup("Design"), Tags(TagsCategory.Location)]
        public string[] tags = Array.Empty<string>();

        [FoldoutGroup("Design"), RichEnumExtends(typeof(LocationInteractability))]
        public RichEnumReference startInteractabilityReference;

        [FoldoutGroup("Prefab"), HideIf("@gameObject.isStatic || " + nameof(_hidableStatic)),
         ARAssetReferenceSettings(new[] { typeof(GameObject) }, true, AddressableGroup.Locations)]
        public ARAssetReference prefabReference;

        [FoldoutGroup("Prefab"), LabelText("Snap To Ground On Spawn")]
        public bool snapToGround = true;

        [FoldoutGroup("Prefab"), HideIf("@gameObject.isStatic || " + nameof(_hidableStatic)), SerializeField]
        bool _isNonMovable;

        [FoldoutGroup("Prefab"), SerializeField]
        bool _hidableStatic;

        [FoldoutGroup("Advanced")] [UnityEngine.Scripting.Preserve] public float maxActiveDistanceBand = 3;

        // === Attachment Group
        public string AttachGroupId => Template.DefaultAttachmentGroupName;
        public bool StartEnabled => true;

        // === Queries
        public ICollection<string> Tags => tags;

        public LocationInteractability StartInteractability =>
            startInteractabilityReference.EnumAs<LocationInteractability>() ?? LocationInteractability.Active;

        public bool IsNonMovable => _isNonMovable;
        public bool IsHidableStatic => _hidableStatic;
        public IModel Model => GetComponentInChildren<VLocation>(true)?.Target;

        public Transform Transform => transform;
        public bool ProvideRepresentationOptions => IsHidableStatic;

        // === Methods
        /// <summary>
        /// Used by SpecSpawner to create locations that are placed on the Scene. 
        /// </summary>
        public override Model CreateModel() {
            return LocationCreator.CreateSceneLocation(this);
        }

        public string GetLocationId() => SceneId.FullId;

        public IEnumerable<IAttachmentSpec> GetAttachments() {
            // return only attachments that are not under attachment groups
            PooledList<IAttachmentSpec>.Get(out var attachmentsSpecs);
            GetComponentsInChildren<IAttachmentSpec>(true, attachmentsSpecs);
            
            foreach (var attachment in attachmentsSpecs.value) {
                Component component = (Component)attachment;
                IAttachmentGroup parentGroup = ExtractParentGroup(component: component);
                if (ReferenceEquals(parentGroup, this)) {
                    yield return attachment;
                }
            }
            attachmentsSpecs.Release();
        }

        IAttachmentGroup ExtractParentGroup(Component component) {
            IAttachmentGroup parentGroup = component.GetComponentInParent<IAttachmentGroup>(true);
            if (parentGroup is LocationTemplate locationTemplate) {
                return locationTemplate.GetComponent<LocationSpec>();
            }

            return parentGroup;
        }

        public IEnumerable<IAttachmentGroup> GetAttachmentGroups() {
            foreach (var group in GetComponentsInChildren<IAttachmentGroup>(true)) {
                Component component = (Component) group;

                if (component.GetComponentInParent<LocationSpec>(true) == this) {
                    yield return group;
                }
            }
        }
        
        public IWithUnityRepresentation.Options GetRepresentationOptions() {
            return new IWithUnityRepresentation.Options() {
                linkedLifetime = true,
                movable = (_isNonMovable | _hidableStatic) ? false : null,
            };
        }

        // === Editor
#if UNITY_EDITOR
        bool _previewWasDisabled;
        GameObject _editorPrefabInstance;
        string _editorPrefabGuid;
        public GameObject EDITOR_GameObject { get; private set; }

        [HideIf(nameof(_hidableStatic))]
        public bool autoConvertToStatic = true;
        
        public static bool AllPreviewsEnabled {
            get => EditorPrefs.GetBool("disableAllPreviews", false) == false;
            set => EditorPrefs.SetBool("disableAllPreviews", !value);
        }
        
        public static bool LocationPreviewsEnabled {
            get => EditorPrefs.GetBool("disableLocationPreviews", false) == false;
            set => EditorPrefs.SetBool("disableLocationPreviews", !value);
        }
        
        static bool PreviewIsDisabled => EditorPrefs.GetBool("disableAllPreviews", false) ||
                                         EditorPrefs.GetBool("disableLocationPreviews", false);
        public GameObject EditorPrefabInstance => _editorPrefabInstance;
        
        void Awake() {
            if (Application.isPlaying) {
                return;
            }

            ValidateStaticState();
            ValidatePrefab(true);
        }

        void OnEnable() {
            if (Application.isPlaying) {
                return;
            }

            RemoveAllRedundantInstances();
            EDITOR_GameObject = gameObject;
            UnityUpdateProvider.GetOrCreate().EDITOR_Register(this);
        }

        void OnDisable() {
            UnityUpdateProvider.GetOrCreate().EDITOR_Unregister(this);
        }

        public void UnityEditorLateUpdate(bool selected) {
            if (_previewWasDisabled != PreviewIsDisabled) {
                ValidateStaticState();
                _previewWasDisabled = PreviewIsDisabled;
            }
            if (!selected) return;

            ValidatePrefabReference();
            ValidatePrefab(true);
        }

        public void EDITOR_SetHideableStatic(bool value) {
            _hidableStatic = value;
            EditorUtility.SetDirty(gameObject);
        }

        void ValidateStaticState() {
            if (prefabReference is not { IsSet: true }) {
                if (IsHidableStatic) {
                    UpdateStaticState(false);
                    autoConvertToStatic = false;
                    return;
                }

                if (!autoConvertToStatic) return;
                // Auto mark location as static, if someone wants to change that, they need to uncheck static and assign Prefab Reference.  
                UpdateStaticState(true);
            }
        }

        void UpdateStaticState(bool state) {
            foreach (var t in GetComponentsInChildren<Transform>(true)) {
                t.gameObject.isStatic = state;
            }

            EditorUtility.SetDirty(gameObject);
        }

        void ValidatePrefabReference() {
            if ((gameObject.isStatic || IsHidableStatic) && prefabReference is { IsSet: true }) {
                prefabReference = null;
                EditorUtility.SetDirty(gameObject);
            }
        }

        public void ValidatePrefab(bool allowRemoval) {
            if (!PreviewIsDisabled && TryGetGuid(out string guid)) {
                if (guid == _editorPrefabGuid) return;
                _editorPrefabGuid = guid;

                if (_editorPrefabInstance != null) {
                    GameObjects.DestroySafely(_editorPrefabInstance);
                    LocationSpecPreviewManager.UnregisterPreview(this);
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) return;
                _editorPrefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
                _editorPrefabInstance.transform.localPosition = Vector3.zero;
                LocationSpecPreviewManager.RegisterPreview(this, _editorPrefabInstance);

                PrefabUtility.GetPrefabInstanceHandle(_editorPrefabInstance).hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;

                if (allowRemoval) {
                    RemoveAllRedundantInstances();
                }
            } else if (_editorPrefabInstance != null) {
                GameObjects.DestroySafely(_editorPrefabInstance);
                LocationSpecPreviewManager.UnregisterPreview(this);
                _editorPrefabGuid = null;
                _editorPrefabInstance = null;
            }
        }

        public bool TryGetGuid(out string guid) {
            var npcAttachment = GetComponentInChildren<NpcAttachment>();
            if (npcAttachment != null) {
                return TryGetGuidFrom(npcAttachment.VisualPrefab, out guid);
            }

            var npcPresenceAttachment = GetComponentInChildren<NpcPresenceAttachment>();
            if (npcPresenceAttachment != null) {
                return TryGetGuidFrom(npcPresenceAttachment.Template?.GetComponentInChildren<NpcAttachment>()?.VisualPrefab, out guid);
            }

            return TryGetGuidFrom(prefabReference, out guid);

            static bool TryGetGuidFrom(ARAssetReference reference, out string guid) {
                var result = reference is { IsSet: true };
                guid = result ? reference.Address : null;
                return result;
            }
        }

        void RemoveAllRedundantInstances() {
            if (_editorPrefabInstance == null) return;
            string spawnedInstancePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_editorPrefabInstance);
            List<GameObject> children = new();
            foreach (Transform child in transform) {
                children.Add(child.gameObject);
            }

            foreach (var child in children.Where(c => c != _editorPrefabInstance)) {
                string childPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(child);
                if (childPath == spawnedInstancePath) {
                    GameObjects.DestroySafely(child);
                }
            }
        }

        // === Possible Attachments (EDITOR)
        const string PossibleAttachmentsGroup = "Possible Attachments";

        static Dictionary<AttachmentCategory, PossibleAttachmentsGroup> s_possibleAttachments;
        static Dictionary<AttachmentCategory, PossibleAttachmentsGroup> PossibleAttachments => s_possibleAttachments ??= PossibleAttachmentsUtil.Get(typeof(LocationSpec));

        [FoldoutGroup(PossibleAttachmentsGroup, order: 999, expanded: true), ShowInInspector, HideReferenceObjectPicker]
        [LabelText(nameof(AttachmentCategory.Common), icon: SdfIconType.StarFill, IconColor = ARColor.EditorLightYellow)]
        PossibleAttachmentsGroup CommonGroup {
            get => PossibleAttachments.TryGetValue(AttachmentCategory.Common, out var group) ? group.WithContext(this) : null;
            set => PossibleAttachments[AttachmentCategory.Common] = value;
        }

        [FoldoutGroup(PossibleAttachmentsGroup), ShowInInspector, HideReferenceObjectPicker]
        [LabelText(nameof(AttachmentCategory.Rare), icon: SdfIconType.InfoCircleFill, IconColor = ARColor.EditorMediumBlue)]
        PossibleAttachmentsGroup RareGroup {
            get => PossibleAttachments.TryGetValue(AttachmentCategory.Rare, out var group) ? group.WithContext(this) : null;
            set => PossibleAttachments[AttachmentCategory.Rare] = value;
        }

        [FoldoutGroup(PossibleAttachmentsGroup), ShowInInspector, HideReferenceObjectPicker]
        [LabelText(nameof(AttachmentCategory.ExtraCustom), icon: SdfIconType.InfoCircleFill, IconColor = ARColor.EditorDarkBlue, NicifyText = true)]
        PossibleAttachmentsGroup CustomGroup {
            get => PossibleAttachments.TryGetValue(AttachmentCategory.ExtraCustom, out var group) ? group.WithContext(this) : null;
            set => PossibleAttachments[AttachmentCategory.ExtraCustom] = value;
        }

        [FoldoutGroup(PossibleAttachmentsGroup), ShowInInspector, HideReferenceObjectPicker]
        [LabelText(nameof(AttachmentCategory.Technical), icon: SdfIconType.InfoCircleFill, IconColor = ARColor.EditorLightBrown, NicifyText = true)]
        PossibleAttachmentsGroup TechnicalGroup {
            get => PossibleAttachments.TryGetValue(AttachmentCategory.Technical, out var group) ? group.WithContext(this) : null;
            set => PossibleAttachments[AttachmentCategory.Technical] = value;
        }
        
        [MenuItem("TG/Debug/Previews/Enable All Previews")]
        public static void EnableAllPreviews() {
            AllPreviewsEnabled = true;
        }
        
        [MenuItem("TG/Debug/Previews/Disable All Previews")]
        public static void DisableAllPreviews() {
            AllPreviewsEnabled = false;
        }
        
        [MenuItem("TG/Debug/Previews/Enable Location Previews")]
        public static void EnableLocationPreviews() {
            LocationPreviewsEnabled = true;
        }
        
        [MenuItem("TG/Debug/Previews/Disable Location Previews")]
        public static void DisableLocationPreviews() {
            LocationPreviewsEnabled = false;
        }
#endif
    }
}