using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Graphics;
using Awaken.TG.Graphics.Culling;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Quests;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.VisualScripts.Units.Events;
using Awaken.TG.VisualScripts.Units.Utils;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Locations {
    /// <summary>
    /// Model that represents locations that can be entered and taken over by heroes.
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    public sealed partial class Location : Model, ITagged, INamed, ICrimeSource, IInteractableWithHero, ICullingSystemRegistreeModel, IItemOwner, IQuest3DMarkerLocationTarget, IWithStats, IWithDomainMovedCallback, ITimeDependentDisabler {
        public override ushort TypeForSerialization => SavedModels.Location;

        public static readonly Vector3 VectorZero = Vector3.zero;
        public static readonly Quaternion QuaternionIdentity = Quaternion.identity;

        public override Domain DefaultDomain => Domain.CurrentScene();

        // === State
        [Saved] List<string> _tags;
        [Saved] public bool Cleared { get; private set; }

        // --- Used only for saving and loading position and rotation of location
        [Saved] public Vector3 SavedCoords { get; private set; } = Vector3.zero;
        [Saved] public Quaternion SavedRotation { get; private set; } = new(0, 0, 0, 0);

        [Saved] LocationInteractability _interactability;
        [Saved] LocationInitializer _initializer;

        [Saved] AttachmentTracker _attachmentTracker;

        public Transform ViewParent { get; set; }
        Registree Registree { get; set; }

        public ICollection<string> Tags => _tags;
        public LocationInitializer Initializer => _initializer;
        public LocationSpec Spec => _initializer.Spec;
        public LocationTemplate Template => _initializer.Template;

        public bool ValidAfterUpdate => _initializer != null && Template != null && Template.GetComponent<LocationSpec>() != null;

        public Vector3 SpecInitialPosition => _initializer.SpecInitialPosition;
        public Quaternion SpecInitialRotation => _initializer.SpecInitialRotation;
        [UnityEngine.Scripting.Preserve] public Vector3 SpecInitialScale => _initializer.SpecInitialScale;

        public ARAssetReference OverridenLocationPrefab => _initializer.OverridenLocationPrefab;

        Vector3 _coords;
        Quaternion _rotation;

        Action<Transform> _afterVisualLoaded;
        Action<Transform> _afterVisualLoadedVS;
        Transform _visual;
        bool _isLoading;
        VLocation _locationView;

        // === Properties
        public Vector3 Coords {
            get => _coords;
            private set {
                SavedCoords = value;
                _coords = value;
                if (ViewParent != null) {
                    ViewParent.position = value;
                }
            }
        }

        public Quaternion Rotation {
            get => _rotation;
            private set {
                SavedRotation = value;
                _rotation = value;
                if (ViewParent != null) {
                    ViewParent.rotation = value;
                }
            }
        }

        public LocString SpecDisplayName => _specDisplayName ??= Spec.displayName;
        public string DisplayName => GetOverridableName(string.IsNullOrWhiteSpace(OverrideName) ? SpecDisplayName : OverrideName);
        public string DebugName => Spec != null ? Spec.name : (Template != null ? Template.name : "No Spec or Template");
        public VLocation LocationView => CachedView(ref _locationView);

        string OverrideName => _overridenName ??= (_initializer as RuntimeLocationInitializer)?.OverridenLocationName;
        LocString _specDisplayName;
        string _overridenName;

        public ARAssetReference CurrentPrefab => OverridenLocationPrefab?.IsSet ?? false ? OverridenLocationPrefab : Spec.prefabReference;

        public bool IsStatic => CurrentPrefab is not { IsSet: true };
        public bool IsNonMovable => IsStatic || Spec.IsNonMovable || Spec.IsHidableStatic;
        public Vector3 SpecInitialForward => SpecInitialRotation * Vector3.forward;
        
        // === ITimeDependantDisabler
        bool ITimeDependentDisabler.TimeUpdatesDisabled => _timeUpdateDisabledByDistance || _timeUpdateDisabledByHidden;
        bool _timeUpdateDisabledByDistance;
        bool _timeUpdateDisabledByHidden;
        
        // === IQuest3DMarkerLocationTarget
        public VLocation StickToReferenceOverride { get; set; }
        public VLocation StickToReference => StickToReferenceOverride ? StickToReferenceOverride : LocationView;

        // === IItemOwner
        /// <summary>
        /// Prefers inventories that can be stolen from
        /// </summary>
        [CanBeNull] public IInventory Inventory {
            get {
                IInventory theftInventory = null;
                foreach (var inventory in Elements<IInventory>()) {
                    if (inventory.CanBeTheft) {
                        theftInventory = inventory;
                        break;
                    }
                    theftInventory ??= inventory;
                }
                return theftInventory ?? TryGetElement<IItemOwner>()?.Inventory;
            }
        }

        public ICharacter Character => TryGetElement<ICharacter>();
        public IEquipTarget EquipTarget => TryGetElement<INpcEquipTarget>();

        // === Visibility and revealing
        public LocationInteractability Interactability {
            get => _interactability ??= LocationInteractability.Hidden;
            private set {
                _interactability = value ?? Interactability;
                _timeUpdateDisabledByHidden = _interactability == LocationInteractability.Hidden;
            }
        }

        public bool IsVisualLoaded { get; private set; }
        public bool? IsCulled { get; private set; }
        public bool IsVisited => TryGetElement<IVisitationProvider>()?.IsVisited ?? false;
        public bool VisibleToPlayer => Interactability != LocationInteractability.Hidden;
        public bool IsBusy => HasElement<Busy>();
        bool IsRuntimeSpawned => _initializer is RuntimeLocationInitializer;

        bool HasMoved => SavedCoords != SpecInitialPosition || SavedRotation != SpecInitialRotation;

        public bool Interactable => Interactability.interactable;
        public GameObject InteractionVSGameObject => Spec.gameObject;
        public Vector3 InteractionPosition => Coords;

        Vector3 ICrimeSource.Position => Coords;

        CrimeOwnerTemplate _cachedDefaultCrimeOwner;
        public CrimeOwnerTemplate DefaultOwner => TryGetElement<CrimeOwnerOverride>()?.CrimeOwner ??
                                                  (_cachedDefaultCrimeOwner ??= NpcTemplate.FromNpcOrDummy(this)?.DefaultCrimeOwner);
        Faction ICrimeSource.Faction => TryGetElement<IWithFaction>()?.Faction;

        public bool IsNoCrime(in CrimeArchetype archetype) => ICrimeDisabler.IsCrimeDisabled(this, in archetype);

        ref readonly CrimeArchetype ICrimeSource.OverrideArchetype(in CrimeArchetype archetype) {
            if (TryGetElement(out CrimeOverride @override)) {
                return ref @override.Override(in archetype);
            } else {
                return ref archetype;
            }
        }
        float ICrimeSource.GetBountyMultiplierFor(in CrimeArchetype archetype) => 1;

        // === Events
        public new static class Events {
            public static readonly Event<Location, LocationInteractability> InteractabilityChanged = new(nameof(InteractabilityChanged));
            public static readonly Event<Location, GameObject> BeforeVisualLoaded = new(nameof(BeforeVisualLoaded));
            public static readonly Event<Location, GameObject> VisualLoaded = new(nameof(VisualLoaded));
            public static readonly Event<Location, bool> LocationVisibilityChanged = new(nameof(LocationVisibilityChanged));
            public static readonly Event<Location, LocationInteractionData> Interacted = new(nameof(Interacted));
            public static readonly Event<Location, LocationInteractionData> AfterInteracted = new(nameof(AfterInteracted));
            public static readonly Event<Location, LocationInteractionData> InteractionFinished = new(nameof(InteractionFinished));
            public static readonly Event<Location, Location> LocationCleared = new(nameof(LocationCleared));
            public static readonly Event<Location, Item> ItemPickedFromLocation = new(nameof(ItemPickedFromLocation));
            public static readonly Event<Location, Location> AnyItemPickedFromLocation = new(nameof(AnyItemPickedFromLocation));

        }

        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve] Location() { }
        public Location(LocationInitializer initializer) {
            _initializer = initializer;
        }

        public static Location JsonCreate() => new Location();

        // === Setups
        // Called before MVC Initialization phase <br/>
        public void Setup(LocationSpec spec) {
            // The ILocationInitializer element is added to Location in the LocationTemplate when the Location instance is created
            _initializer.PrepareSpec(this);
            ViewParent = _initializer.PrepareViewParent(this);
            SetSpecProperties(spec);
        }

        // === Initialization
        protected override void OnAfterDeserialize() {
            // if we didn't save initializer it means it was SceneLocationInitializer with ShouldBeSaved set to false (see WriteSavables)
            _initializer ??= new SceneLocationInitializer();
            _attachmentTracker.SetOwner(this);
        }

        protected override void OnPreRestore() {
            _initializer.PrepareSpec(this);
            _attachmentTracker.PreRestore(Spec.GetAttachmentGroups());
        }

        protected override void OnInitialize() {
            _attachmentTracker = new AttachmentTracker();
            _attachmentTracker.SetOwner(this);
            _attachmentTracker.Initialize(Spec.GetAttachmentGroups());
            Init();
        }

        protected override void OnRestore() {
            ViewParent = _initializer.PrepareViewParent(this);
            RestoreSpecProperties(Spec);
            Init();
        }

        protected override void OnFullyInitialized() {
            SafeGraph.Trigger(AfterLocationInitializedUnit.Hook, Spec.gameObject);
        }

        void Init() {
            if (_isLoading) {
                return;
            }

            LoadingStates.LoadingLocations++;
            _isLoading = true;
            _initializer.Init(this);
        }

        // === Persistence

        public override void Serialize(SaveWriter writer) {
            base.Serialize(writer);

            if (Spec.tags.SequenceEqual(_tags) == false) {
                writer.WriteName(SavedFields._tags);
                writer.WriteList(_tags, static (writer, tag) => writer.Write(tag));
                writer.WriteSeparator();
            }
            
            if (HasMoved && !IsNonMovable) {
                writer.WriteName(SavedFields.SavedCoords);
                writer.Write(SavedCoords);
                writer.WriteSeparator();
                writer.WriteName(SavedFields.SavedRotation);
                writer.Write(SavedRotation);
                writer.WriteSeparator();
            }

            if (Spec.StartInteractability != _interactability) {
                writer.WriteName(SavedFields._interactability);
                writer.WriteRichEnum(_interactability);
                writer.WriteSeparator();
            }

            if (Cleared) {
                writer.WriteName(SavedFields.Cleared);
                writer.Write(Cleared);
                writer.WriteSeparator();
            }
            
            writer.WriteName(SavedFields._attachmentTracker);
            writer.Write(_attachmentTracker);
            writer.WriteSeparator();

            if (_initializer.ShouldBeSaved) {
                writer.WriteName(SavedFields._initializer);
                writer.Write(_initializer);
                writer.WriteSeparator();
            }
        }

        public void AddTag(string tag) {
            if (_tags.Contains(tag) == false) {
                _tags.Add(tag);
            }
        }

        void SetSpecProperties(LocationSpec spec) {
            Interactability = spec.StartInteractability;
            _tags = new List<string>(spec.tags.Length);
            _tags.AddRange(spec.tags);
            PrepareBasedOnSpec();
        }

        void RestoreSpecProperties(LocationSpec spec) {
            if (_tags == null) {
                _tags = new List<string>(spec.tags.Length);
                _tags.AddRange(spec.tags);
            } else {
                foreach (var tag in spec.tags) {
                    if (_tags.Contains(tag) == false) {
                        _tags.Add(tag);
                    }
                }
            }
            if (_interactability == null) {
                _interactability = spec.StartInteractability;
                _timeUpdateDisabledByHidden = _interactability == LocationInteractability.Hidden;
            }
            PrepareBasedOnSpec();
        }

        void PrepareBasedOnSpec() {
            if (IsNonMovable || SavedRotation.Equals(new Quaternion(0, 0, 0, 0))) {
                SavedCoords = SpecInitialPosition;
                SavedRotation = SpecInitialRotation;
            } else if (SavedCoords.IsInvalid()) {
                Log.Critical?.Error($"Model {LogUtils.GetDebugName(this)} has invalid SavedCoords, setting to SpecInitialPosition");
                SavedCoords = SpecInitialPosition;
                SavedRotation = SpecInitialRotation;
            }

            _coords = SpecInitialPosition;
            _rotation = SpecInitialRotation;

            ViewParent.gameObject.AddComponent<LocationParent>();
        }

        // Visual Loading
        public void OnVisualLoaded(Action<Transform> callback, bool visualScripting = false) {
            if (IsVisualLoaded) {
                callback.Invoke(_visual);
            } else {
                if (visualScripting) {
                    _afterVisualLoadedVS += callback;
                } else {
                    _afterVisualLoaded += callback;
                }
            }
        }

        public void VisualLoaded(Transform transform, VLocation.LocationVisualSource visualSource) {
            IsVisualLoaded = true;
            _visual = transform;
            MainView.gameObject.tag = transform.gameObject.tag;

            _afterVisualLoaded?.Invoke(_visual);
            _afterVisualLoadedVS?.Invoke(_visual);

            _afterVisualLoaded = null;
            _afterVisualLoadedVS = null;

            if (visualSource != VLocation.LocationVisualSource.FromScene && IsNonMovable && _visual) {
                World.Services.Get<DistanceCullersService>().Register(_visual.gameObject);
            }

            if (_isLoading) {
                LoadingStates.LoadingLocations--;
                _isLoading = false;
            } else {
                Log.Critical?.Error($"Location {ID} visual loaded without being in loading state!");
            }

            this.Trigger(Events.VisualLoaded, _visual?.gameObject);
        }

        public void VisualLoadingFailed() {
            if (_isLoading) {
                LoadingStates.LoadingLocations--;
                _isLoading = false;
            } else {
                Log.Critical?.Error($"Location {ID} visual failed to load without being in loading state!");
            }
        }

        public void DomainMoved(Domain newDomain) {
            ViewParent.SetParent(Services.Get<ViewHosting>().LocationsHost(newDomain));
        }

        // === Actions
        public IHeroAction DefaultAction(Hero hero) => AvailableActions(hero)
            .FirstOrDefault(a => a.GetAvailability(hero, this) == ActionAvailability.Available);

        public IEnumerable<IHeroAction> AvailableActions(Hero hero) {
            bool actionsUnavailable = HasBeenDiscarded || (hero != null && World.All<Story>().Any(s => s.InvolveHero));
            return actionsUnavailable ? Enumerable.Empty<IHeroAction>() : HeroInteraction.ActionsFromLocation(this);
        }

        public void DestroyInteraction() {
            Discard();
        }

        // === Operations
        public void MoveAndRotateTo(Vector3 coords, Quaternion rotation, bool teleport = false) {
            bool moved = MoveTo(coords, teleport);
            bool rotated = RotateTo(rotation);
            if (moved) {
                this.Trigger(GroundedEvents.AfterMoved, this);
                this.Trigger(GroundedEvents.AfterMovedToPosition, coords);
                if (teleport) {
                    this.Trigger(GroundedEvents.AfterTeleported, this);
                }
            } else if (rotated) {
                this.Trigger(GroundedEvents.AfterMoved, this);
            }
        }

        bool MoveTo(Vector3 coords, bool teleport) {
            if (coords == Coords) {
                return false;
            }
#if UNITY_EDITOR
            if (IsNonMovable && !HasElement<Elevator.ElevatorPlatform>()) {
                Log.Minor?.Error($"Trying to move non-movable location {LogUtils.GetDebugName(this)}");
            }
#endif
            if (teleport) {
                this.Trigger(GroundedEvents.BeforeTeleported, this);
            }

            Coords = coords;
            Registree?.UpdateOwnPosition();
            return true;
        }

        bool RotateTo(Quaternion rotation) {
            if (rotation == Rotation) {
                return false;
            }

            Rotation = rotation;
            return true;
        }

        public void SetCoordsBeforeSave(Vector3 coords) {
            SavedCoords = coords;
        }

        public void SetInteractability(LocationInteractability interactability) {
            if (Interactability == interactability) {
                return;
            }
            
            Interactability = interactability;
            
            this.Trigger(Events.InteractabilityChanged, Interactability);
            TriggerChange();
        }

        public void Kill(ICharacter killer = null, bool markDeathAsNonCriminal = false, bool allowPrevention = false) {
            OnVisualLoaded(InternalKill);
            void InternalKill(Transform parentTransform) {
                IAlive alive = TryGetElement<IAlive>();
                if (alive == null) {
                    return;
                }
                if (markDeathAsNonCriminal) {
                    this.AddMarkerElement<NonCriminalDeathMarker>();
                }
                alive.Kill(killer, allowPrevention);
            }
        }

        public void Clear() {
            if (!Cleared) {
                Cleared = true;
                this.Trigger(Events.LocationCleared, this);
            }
        }

        public void TriggerVisualScriptingEvent(string action, params object[] parameters) {
            LocationView.TriggerVisualScriptingEvent(action, parameters);
        }

        public void TriggerVisualScriptingEvent(VSCustomEvent action, params object[] parameters) {
            TriggerVisualScriptingEvent(action.ToString(), parameters);
        }

        public void StopEmittingSounds() {
            LocationView?.StopEmittingSounds();
        }

        public void SetCulled(bool culled) {
            if (IsCulled == culled) {
                return;
            }

            IsCulled = culled;
            if (LocationView is IVLocationWithState locationViewWithState) {
                locationViewWithState.UpdateState();
            }
        }

        // === Attachment Groups
        public void DisableGroup(string groupName) {
            _attachmentTracker.DisableGroup(groupName);
        }

        public void EnableGroup(string groupName) {
            IAttachmentGroup group = Spec.GetAttachmentGroups().FirstOrDefault(g => g.AttachGroupId == groupName);
            if (group == null) {
                Log.Important?.Error($"Invalid group name: {groupName}, it doesn't exist in Location {LogUtils.GetDebugName(this)}", Spec.gameObject);
                return;
            }
            _attachmentTracker.EnableGroup(group, group.GetAttachments());
        }

        // === Overridable variables
        public string GetOverridableName(string original) {
            return Elements<ILocationNameModifier>().GetManagedEnumerator()
                .OrderBy(mod => mod.ModificationOrder)
                .Aggregate(original, (t, mod) => mod.ModifyName(t));
        }

        // === ICullingSystem Registree
        public void CullingSystemBandUpdated(int newDistanceBand) {
            _timeUpdateDisabledByDistance = !LocationCullingGroup.InActiveLogicBands(newDistanceBand);
        }

        public Registree GetRegistree() {
            if (TryGetElement<LocationRegistreeTypeOverride>(out var overrideType)) {
                return Registree = overrideType.GetRegistree(this);
            }

            return Registree = Registree.ConstructFor<LocationCullingGroup>(this).Build();
        }

        public Stat Stat(StatType statType) {
            return Elements<IWithStats>()
                .Select(withStats => withStats.Stat(statType))
                .WhereNotNull()
                .FirstOrDefault();
        }

        // === Discard
        public const string DiscardedPlacesKey = "discarded.places";
        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop && !IsRuntimeSpawned) {
#if UNITY_EDITOR || AR_DEBUG
                if ((IsStatic || Spec.IsHidableStatic) && (RenderUtils.HasAnyRenderer(ViewParent) || LocationUtils.HasAnyLocationIndependentLogic(ViewParent, true))) {
    #if UNITY_EDITOR
                    throw new InvalidOperationException($"Trying to discard Static Location with logic or renderers {LogUtils.GetDebugName(this)} {ViewParent.name}");
    #else
                    Log.Critical?.Error($"Trying to discard Static Location with logic or renderers {LogUtils.GetDebugName(this)} {ViewParent.name}");
    #endif
                }
#endif
                Services.Get<GameplayMemory>().Context(DiscardedPlacesKey).Set(ID, true);
            }
            if (!IsStatic && ViewParent != null) {
                Object.Destroy(ViewParent.gameObject);
            }
            if (_isLoading) {
                LoadingStates.LoadingLocations--;
                _isLoading = false;
            }
        }
    }
}