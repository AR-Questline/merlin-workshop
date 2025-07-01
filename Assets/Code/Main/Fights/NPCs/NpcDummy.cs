using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using Awaken.Kandra;
using Awaken.TG.Assets;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.AI.Combat.CustomDeath;
using Awaken.TG.Main.AI.Grid;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Attachments.Elements.DeathBehaviours;
using Awaken.TG.Main.Locations.Elevator;
using Awaken.TG.Main.Locations.Mobs;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Locations.Views;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Animations;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.GameObjects;
using Awaken.VendorWrappers.Salsa;
using CrazyMinnow.SALSA;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Pathfinding;
using Pathfinding.RVO;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Main.Fights.NPCs {
    public partial class NpcDummy : Element<Location>, INpcEquipTarget, IGrounded {
        public override ushort TypeForSerialization => SavedModels.NpcDummy;

        public Transform ParentTransform { get; private set; }
        
        [Saved] public NpcTemplate Template { get; private set; }
        [Saved] ARAssetReference _visualReference;
        [Saved] ShareableARAssetReference _simplifiedDeadBodyReplacementRef;
        [Saved(false)] bool _fromAttachment;
        [Saved(false)] bool _hasDied, _disableLoot, _hasBody;
        [Saved] ItemSpawningData[] _initialItems;

        [UnityEngine.Scripting.Preserve] Transform _mainHand, _offHand, _head, _torso, _rootBone;
        // --- For Initialization Only
        NpcElement _npcElement;
        bool _itemAddedListenerRegistered;
        // --- For Discarding Only
        ReferenceInstance<GameObject> _spawnedVisualPrefab;
        NpcGenderMarker _npcGenderMarker;
        IEventListener _addItemListener;
        
        bool _dummyCompletelyInitialized;
        DummyCompletelyInitialized _onDummyCompletelyInitialized;
        
        public NpcChunk NpcChunk { get; set; }
        
        // === IGrounded
        public Vector3 Coords => ParentModel.Coords;
        public Quaternion Rotation => ParentModel.Rotation;

        // === IItemOwner
        public IInventory Inventory => ParentModel.TryGetElement<IInventory>();
        public ICharacter Character => null;
        public IEquipTarget EquipTarget => TryGetElement<IEquipTarget>();
        public NpcClothes NpcClothes => ParentModel.TryGetElement<NpcClothes>();
        public bool CanEquip => _hasBody && LocationPrefabNotOverriden;
        public IBaseClothes<IItemOwner> Clothes => NpcClothes;
        public IView BodyView => ParentModel.LocationView;
        public Transform Head => _head;
        public Transform Torso => _torso;
        // === IWithItemSockets
        public Transform HeadSocket => _head;
        public Transform MainHandSocket => _mainHand;
        public Transform OffHandSocket => _offHand;
        public Transform HipsSocket => _rootBone;
        public Transform RootSocket => ParentTransform;
        public Transform Neck { get; private set; }

        // === Other
        [UnityEngine.Scripting.Preserve] bool DoesntHaveImportantItems => ParentModel.TryGetElement<SearchAction>()?.AvailableTemplates.All(i => !i.IsImportantItem) ?? false;
        bool LocationPrefabNotOverriden => ParentModel.OverridenLocationPrefab?.IsSet is false or null;
        public bool HasDied => _hasDied;

        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        NpcDummy() {}

        public NpcDummy(NpcElement npcElement, ReferenceInstance<GameObject> spawnedVisualPrefab, ShareableARAssetReference simplifiedDeadBodyReplacementRef, bool disableLoot = false, bool hasDied = true, bool hasBody = true) {
            Template = npcElement.Template;
            _npcElement = npcElement;
            _spawnedVisualPrefab = spawnedVisualPrefab;
            _visualReference = hasBody ? spawnedVisualPrefab.Reference : null;
            _simplifiedDeadBodyReplacementRef = simplifiedDeadBodyReplacementRef;
            _hasDied = hasDied;
            _disableLoot = disableLoot;
            _hasBody = hasBody;
            _fromAttachment = false;
        }

        public NpcDummy(NpcTemplate template, ItemSpawningData[] initialItems) {
            Template = template;
            _initialItems = initialItems;
            _hasBody = true;
            _fromAttachment = true;
        }
        
        // === Initialization
        protected override void OnInitialize() {
            AddElement(new BodyFeatures());
            this.ListenTo(Events.AfterFullyInitialized, () => ParentModel.OnVisualLoaded(t => AfterVisualLoaded(t, false).Forget()));
        }
        
        protected override void OnRestore() {
            this.ListenTo(Events.AfterFullyInitialized, () => ParentModel.OnVisualLoaded(t => AfterVisualLoaded(t, true).Forget()));
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            _spawnedVisualPrefab?.ReleaseInstance();
            _spawnedVisualPrefab = null;
        }

        protected override bool OnSave() {
            // if Location has MovingPlatform element it is already in the right position
            if (_hasDied && _rootBone != null && !ParentModel.HasElement<MovingPlatform>()) {
                // Snap location position to the visual dead body position
                Vector3 position = Ground.SnapToGround(_rootBone.position);
                ParentModel.SetCoordsBeforeSave(position);
            }
            return base.OnSave();
        }

        void ProcessOnElevatorUpdate(float deltaTime, ElevatorPlatform elevatorPlatform) {
            ParentModel.SafelyMoveTo(_rootBone.position);
        }

        async UniTaskVoid AfterVisualLoaded(Transform parentTransform, bool isRestoring) {
            ParentTransform = parentTransform;
            
            RegisterItemAddedListener();

            if (LocationPrefabNotOverriden) {
                if (isRestoring) {
                    RemoveMovementControllers(ParentTransform);
                    if (_hasBody && _visualReference is { IsSet: true }) {
                        _spawnedVisualPrefab = new ReferenceInstance<GameObject>(_visualReference);
                        var spawnedGO = await _spawnedVisualPrefab.Instantiate(ParentTransform.Find("Visuals"));
                        if (spawnedGO == null) {
                            _spawnedVisualPrefab?.ReleaseInstance();
                            _spawnedVisualPrefab = null;
                        }
                    }
                }
                
                if (ParentModel.TryGetElement<BaseClothes>() == null) {
                    ParentModel.AddElement(new NpcClothes());
                }

                var features = Element<BodyFeatures>();
                
                if (!isRestoring && _npcElement != null) {
                    features.MoveFrom(_npcElement.Element<BodyFeatures>());
                }
                
                features.Gender = ParentTransform.gameObject.GetComponentInChildren<NpcGenderMarker>(true)?.Gender ?? Gender.None;
                features.InitCovers(Clothes);
            }

            if (_hasBody) {
                Transform[] transforms = ParentTransform.GetComponentsInChildren<Transform>(true);
                GameObject gameObject;
                BodyParts bodyParts = BodyParts.None;
                foreach (var transform in transforms) {
                    gameObject = transform.gameObject;
                    
                    if (gameObject.CompareTag("Neck")) {
                        Neck = transform;
                        continue;
                    }
                    
                    if (!bodyParts.HasFlagFast(BodyParts.MainHand) && gameObject.CompareTag("MainHand")) {
                        _mainHand = transform;
                        bodyParts |= BodyParts.MainHand;
                        if (bodyParts == BodyParts.All) {
                            break;
                        }
                        continue;
                    }

                    if (!bodyParts.HasFlagFast(BodyParts.OffHand) && gameObject.CompareTag("OffHand")) {
                        _offHand = transform;
                        bodyParts |= BodyParts.OffHand;
                        if (bodyParts == BodyParts.All) {
                            break;
                        }
                        continue;
                    }

                    if (!bodyParts.HasFlagFast(BodyParts.Head) && gameObject.CompareTag("Head")) {
                        _head = transform;
                        bodyParts |= BodyParts.Head;
                        if (bodyParts == BodyParts.All) {
                            break;
                        }
                        continue;
                    }

                    if (!bodyParts.HasFlagFast(BodyParts.Torso) && gameObject.CompareTag("Torso")) {
                        _torso = transform;
                        bodyParts |= BodyParts.Torso;
                        if (bodyParts == BodyParts.All) {
                            break;
                        }
                        continue;
                    }

                    if (!bodyParts.HasFlagFast(BodyParts.RootBone) && gameObject.CompareTag("RootBone")) {
                        _rootBone = transform;
                        bodyParts |= BodyParts.RootBone;
                        if (bodyParts == BodyParts.All) {
                            break;
                        }
                        continue;
                    }
                }
            }

            var eyes = ParentTransform.GetComponentInChildren<Eyes>(true);
            if (eyes != null) {
                eyes.enabled = false;
            }

            ParentTransform.GetComponentsInChildren<IEventMachine>(true)
                .ForEach(em => {
                    if (em is Component c) {
                        Object.Destroy(obj: c);
                    }
                });
            
            var animators = ParentTransform.GetComponentsInChildren<Animator>(true);
            animators.ForEach(a => a.writeDefaultValuesOnDisable = false);
            
            if (isRestoring && !_fromAttachment) {
                var alivePrefab = ParentTransform.gameObject.FindChildRecursively("AlivePrefab", true);
                if (alivePrefab != null) {
                    alivePrefab.gameObject.SetActive(false);
                }
                
                CustomDeathController custom = ParentTransform.GetComponentInParent<LocationParent>(true)?.GetComponentInChildren<CustomDeathController>(true);
                CustomDeathAnimation customDeathAnimation = null;
                if (ParentModel.TryGetElement(out IDeathAnimationForwarder forwarder)) {
                    customDeathAnimation = forwarder.CustomDeathAnimation;
                }

                bool shouldPlayDeathAnimation = customDeathAnimation?.animations is { IsSet: true };
                shouldPlayDeathAnimation = shouldPlayDeathAnimation || 
                                           (custom != null && !custom.ShouldRagdollOnDeath && IDeathAnimationProvider.ShouldPlayAnimationAfterLoad(custom.transform));
                if (shouldPlayDeathAnimation) {
                    LoadDeathAnim(ParentTransform.GetComponentInChildren<ARNpcAnimancer>(), customDeathAnimation).Forget();
                    RagdollUtilities.RemoveRagdoll(ParentTransform);
                    ParentModel.GetTimeDependent()?.RemoveInvalidComponentsAfterFrame().Forget();
                } else {
                    animators.ForEach(a => a.enabled = false);
                }
            }
            
            InitializeInventory();
            if (!ParentModel.HasElement<SearchAction>() && !_disableLoot) {
                ParentModel.AddElement(new SearchAction(null, null));
            } else if (ParentModel.HasElement<SearchAction>() && _disableLoot) {
                ParentModel.RemoveElementsOfType<SearchAction>();
            }
            
            _npcElement = null;
            
            // Remove this remove if Dummies can have items added to their inventory during play (e.g. via transfer).
            // bug that needs to be fixed afterwards: when (and only when) looking at dead NPC, it will equip items from loot 
            World.EventSystem.RemoveListener(_addItemListener);
            
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            
            // This is very close logic to the NpcElement, probably could be refactored to shared code
            RefreshDistanceBand(ParentModel.GetCurrentBandSafe(LocationCullingGroup.LastBand));
            foreach (var marker in ParentTransform.GetComponentsInChildren<GameObjectToReenableMarker>(true)) {
                marker.gameObject.SetActive(true);
                Object.Destroy(marker);
            }
            if (HasBeenDiscarded) {
                return;
            }
            ParentModel.ListenTo(ICullingSystemRegistreeModel.Events.DistanceBandChanged, RefreshDistanceBand, this);

            if (HasBeenDiscarded || ParentModel == null) {
                return;
            }

            if (_hasDied && ParentModel.HasElement<MovingPlatform>()) {
                ParentModel.ListenTo(MovingPlatform.Events.MovingPlatformAdded, OnMovingPlatformAdded, this);
                ParentModel.ListenTo(MovingPlatform.Events.MovingPlatformDiscarded, OnMovingPlatformDiscarded, this);
                ParentModel.Element<MovingPlatform>().WithUpdate(ProcessOnElevatorUpdate);
            }

            _dummyCompletelyInitialized = true;
            _onDummyCompletelyInitialized?.Invoke(this);
            _onDummyCompletelyInitialized = null;
        }
        
        public void OnCompletelyInitialized(DummyCompletelyInitialized action) {
            if (_dummyCompletelyInitialized) {
                action(this);
            } else {
                _onDummyCompletelyInitialized += action;
            }
        }
        
        public bool TryReplaceWithSimplifiedLocation() {
            if (!_hasDied) {
                return false;
            }

            if (TryCreateReplacementDeadBody()) {
                ParentModel.Discard();
                return true;
            }
            return false;
        }

        void OnMovingPlatformAdded(MovingPlatform movingPlatform) {
            movingPlatform.WithUpdate(ProcessOnElevatorUpdate);
        }
        
        void OnMovingPlatformDiscarded(MovingPlatform movingPlatform) {
            movingPlatform?.WithoutUpdate(ProcessOnElevatorUpdate);
        }

        void RemoveMovementControllers(Transform parentTransform) {
            var controller = parentTransform.GetComponentInChildren<NpcController>(true);
            if (controller != null) {
                var characterController = controller.GetComponent<CharacterController>();
                var richAI = controller.GetComponent<RichAI>();
                var rvo = controller.GetComponent<RVOController>();
                Object.Destroy(controller);
                    
                if (characterController != null) {
                    Object.Destroy(characterController);
                }
                if (richAI != null) {
                    Object.Destroy(richAI);
                }
                if (rvo != null) {
                    Object.Destroy(rvo);
                }
            }
        }
        
        void RefreshDistanceBand(int band) {
            if (LocationCullingGroup.InBandToDiscardRagdoll(band) && TryReplaceWithSimplifiedLocation()) {
                return;
            }
            if (!HasElement<BodyFeatures>()) {
                return;
            }
            if (ParentTransform != null) {
                if (LocationCullingGroup.InNpcVisibilityBand(band) && ParentModel.VisibleToPlayer) {
                    TurnVisibilityOn();
                } else {
                    TurnVisibilityOff();
                }
            }
            
            Element<BodyFeatures>().RefreshDistanceBand(band);
        }

        bool TryCreateReplacementDeadBody() {
            if (!ParentModel.TryGetElement(out SearchAction currentSearchAction)) {
                return true;
            }
            List<ItemSpawningDataRuntime> itemData = new();
            currentSearchAction.GetAllItems(itemData);
            bool anyVisibleItem = false;
            foreach (var data in itemData) {
                if (!data.ItemTemplate.HiddenOnUI) {
                    anyVisibleItem = true;
                    break;
                }
            }
            if (!anyVisibleItem) {
                // Empty bodies shouldn't create replacement; 
                return true;
            }
            
            if (!LocationPrefabNotOverriden && !ParentTransform.GetComponentInChildren<KandraRenderer>()) {
                // Location prefab is already overriden with a visual containing no Kandra Renderer. No need to create replacement.
                return false;
            }
            
            var replacementParent = World.Services.Get<GameConstants>().DefaultDeadBodyReplacedPrefab;
            var visualAssetRef = _simplifiedDeadBodyReplacementRef is { IsSet: true } ? _simplifiedDeadBodyReplacementRef.Get() : GameConstants.Get.DefaultDeadBodyReplacedVisualPrefab;
            Location location = replacementParent.SpawnLocation(ParentModel.Coords, Quaternion.Euler(0, RandomUtil.UniformFloat(0f, 360f), 0), Vector3.one, visualAssetRef, ParentModel.DisplayName);
            
            var searchAction = new SearchAction(itemData, true);
            location.AddElement(searchAction);
            
            if (ParentModel.TryGetElement(out IWithFaction withFaction)) {
                var factionProvider = new SimpleFactionProvider(withFaction.Faction.Template);
                location.AddElement(factionProvider);
            }

            location.AddElement<DiscardReplacementBodyElement>();
            
            return true;
        }

        void TurnVisibilityOn() {
            if (ParentModel.IsCulled == false) {
                return;
            }
            ParentModel.SetCulled(false);
            if (Inventory is ICharacterInventory characterInventory) {
                foreach (var item in Inventory.Items) {
                    if (item is { HasBeenDiscarded: false, EquipmentType: not null }) {
                        characterInventory.Equip(item);
                    }
                }
            }
            Element<BodyFeatures>().Show();
        }

        void TurnVisibilityOff() {
            if (ParentModel.IsCulled == true) {
                return;
            }
            ParentModel.SetCulled(true);
            if (Inventory is ICharacterInventory characterInventory) {
                foreach (var item in Inventory.Items) {
                    if (item is { HasBeenDiscarded: false, EquipmentType: not null }) {
                        characterInventory.Unequip(item);
                    }
                }
            }
            Element<BodyFeatures>().Hide();
        }
        
        void RegisterItemAddedListener() {
            if (_itemAddedListenerRegistered) return; 
            
            if (!ParentModel.HasElement<ICharacterInventory>()) {
                ParentModel.AddElement(new NpcItems());
            }
            _addItemListener = ParentModel.ListenTo(IItemOwner.Relations.Owns.Events.AfterAttached, ItemAdded, this);
            _itemAddedListenerRegistered = true;
        }

        void InitializeInventory() {
            if (_initialItems == null) {
                return;
            }
            var inventory = ParentModel.TryGetElement<ICharacterInventory>() ?? ParentModel.AddElement(new NpcItems());
            foreach (ItemSpawningData data in _initialItems) {
                ItemTemplate itemTemplate = data.ItemTemplate(this);
                if (itemTemplate == null) {
                    Log.Minor?.Info($"Failed to load item template for NpcDummy '{data.itemTemplateReference.GUID}' in {LogUtils.GetDebugName(this)}");
                    continue;
                }
                inventory.Add(new(itemTemplate, data.quantity, data.ItemLvl));
            }
            _initialItems = null;
        }

        void ItemAdded(RelationEventData data) {
            if (data.to is not Item item) {
                return;
            }
            if (item.IsNPCEquippable && !item.IsEquipped && Inventory is ICharacterInventory characterInventory) {
                characterInventory.Equip(item);
            }
        }

        async UniTaskVoid LoadDeathAnim(AnimancerComponent animancer, [CanBeNull] CustomDeathAnimation customDeathAnimation) {
            ARAssetReference assetReference;
            ITransition clipTransition;
            
            if (customDeathAnimation?.animations is { IsSet: true }) {
                assetReference = customDeathAnimation.animations.Get();
                var result = await assetReference.LoadAsset<ARStateToAnimationMapping>();
                clipTransition = result.GetAnimancerNodes(NpcStateType.CustomDeath).FirstOrDefault();
            } else if (Template.DummyDeathClipTransition is { IsSet: true } assetRef) {
                assetReference = assetRef;
                clipTransition = await assetReference.LoadAsset<ClipTransitionAsset>();
            } else {
                assetReference = Template.FightingStyle.BaseAnimations.Get();
                ARStateToAnimationMapping result = await assetReference.LoadAsset<ARStateToAnimationMapping>();
                ITransition[] transitions = result.GetAnimancerNodes(NpcStateType.Death).ToArray();
                clipTransition = transitions.Length > 0 ? RandomUtil.UniformSelect(transitions) : null;
            }
            
            if (clipTransition != null) {
                animancer.InitializePlayable();
                animancer.Play(clipTransition, 0f, FadeMode.FromStart);
                this.ListenTo(Events.BeforeDiscarded, () => assetReference.ReleaseAsset(), this);
            } else {
                Log.Important?.Error($"Failed to load death animation for NpcDummy: {animancer}", animancer);
                assetReference.ReleaseAsset();
            }
        }
        
        [Flags]
        enum BodyParts : byte {
            None = 0,
            MainHand = 1 << 0,
            OffHand = 1 << 1,
            Head = 1 << 2,
            Torso = 1 << 3,
            RootBone = 1 << 4,
            All = MainHand | OffHand | Head | Torso | RootBone
        }
        
        public delegate void DummyCompletelyInitialized(NpcDummy dummy);
        
        public class GameObjectToReenableMarker : MonoBehaviour { }
    }
}