using System;
using System.Linq;
using System.Threading;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Animations;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    [ExecuteInEditMode]
    public class SimpleInteraction : SimpleInteractionBase {
        const string VfxGroup = InteractingGroup + "/VFX";
        const string SfxGroup = InteractingGroup + "/Audio";
        const string StatusGroup = InteractingGroup + "/Status";

        [SerializeField, FoldoutGroup(AnimationSettingGroup)]
        GesturesSerializedWrapper gestures;

        [SerializeField, FoldoutGroup(AnimationSettingGroup)]
        bool closeEyes;

        [SerializeField, FoldoutGroup(AnimationSettingGroup)]
        EquipInInteraction equipInInteraction;

        [SerializeField, FoldoutGroup(AnimationSettingGroup), ShowIf(nameof(ShowSpecificWeapon))]
        ItemTemplate weaponToUse;

        [SerializeField, FoldoutGroup(AnimationSettingGroup), ShowIf(nameof(ShowSpecificPrefab)),
         ARAssetReferenceSettings(new[] { typeof(GameObject) }, group: AddressableGroup.DroppableItems)]
        ShareableARAssetReference itemToUse;

        [SerializeField, FoldoutGroup(AnimationSettingGroup), ShowIf(nameof(ShowItemHand))]
        CastingHand handToSpawnItem = CastingHand.MainHand;

        [SerializeField, FoldoutGroup(AnimationSettingGroup), ShowIf(nameof(ShowEquipHandOverride))]
        WeaponEquipHand equipHandOverride = WeaponEquipHand.None;
        
        [InfoBox("At the start of the interaction, spawn the chosen GameObject at the Interaction VFX Position. If not set, spawn it at the interaction position.")]
        [ARAssetReferenceSettings(new[] { typeof(GameObject) }, true, AddressableGroup.VFX)] [SerializeField, FoldoutGroup(VfxGroup)]
        ShareableARAssetReference interactionVFX;

        [Tooltip("Spawn the VFX at the position of this object. If not set, spawn it at the interaction position instead.")]
        [SerializeField, FoldoutGroup(VfxGroup)]
        Transform interactionVFXPosition;

        [SerializeField, FoldoutGroup(SfxGroup)]
        public EventReference interactionSFX;

        [SerializeField, FoldoutGroup(DialogueGroup, InterruptingGroupOrder), Tooltip("Should the NPC be allows to look at Hero during interaction?")]
        bool allowGlancing = true;

        [SerializeField, FoldoutGroup(DialogueGroup), ShowIf(nameof(CanTalkInInteraction)), InfoBox("@" + nameof(EditorMaxAngleToTalkInfo))]
        [Tooltip("How should NPC rotate towards Hero? It will impact the max angle to start interaction with that NPC (so you can't talk from his back when he only rotates his head.")]
        SpineRotationType talkRotationType = SpineRotationType.HeadOnly;

        [SerializeField, FoldoutGroup(DialogueGroup), ShowIf(nameof(EditorShowTalkFromBehind))]
        [Tooltip("This NPC doesn't rotate at all, so either he has very small talk angle (60) or you can talk from any side (180).")]
        bool allowTalkingFromBehind;
        
        [SerializeField, FoldoutGroup(StatusGroup), TemplateType(typeof(StatusTemplate))]
        [Tooltip("Status that will be applied to NPC during the interaction")]
        TemplateReference statusTemplate;

        CancellationTokenSource _vfxToken;
        IPooledInstance _vfxInstance;
        bool _interactionVFXSpawned;

        CancellationTokenSource _itemPrefabToken;
        IPooledInstance _usedItemPrefab;
        Item _usedItem;

        Status _status;

        GroundedPosition _currentLookAtTarget;

        public override bool TalkRotateOnlyUpperBody => talkRotationType is SpineRotationType.HeadOnly or SpineRotationType.UpperBody;

        public override GesturesSerializedWrapper Gestures => gestures;
        public override bool AllowGlancing => allowGlancing;
        bool ShowItemHand => itemToUse.IsSet && ShowSpecificPrefab;
        bool ShowSpecificWeapon => equipInInteraction == EquipInInteraction.SpecificWeapon;
        bool ShowSpecificPrefab => equipInInteraction == EquipInInteraction.SpecificPrefab;

        bool ShowEquipHandOverride => equipInInteraction != EquipInInteraction.None &&
                                      equipInInteraction != EquipInInteraction.SpecificPrefab;

        string EditorMaxAngleToTalkInfo => $"Current max angle to talk: {MinAngleToTalk ?? DialogueAction.MinAngleToTalkStanding}";
        bool EditorShowTalkFromBehind => CanTalkInInteraction && talkRotationType == SpineRotationType.None;
        
        public override float? MinAngleToTalk {
            get {
                if (!CanTalkInInteraction) return null;
                return talkRotationType switch {
                    SpineRotationType.FullRotation => null,
                    SpineRotationType.UpperBody => 100,
                    SpineRotationType.HeadOnly => 80,
                    SpineRotationType.None => allowTalkingFromBehind ? null : 60,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        public override SpineRotationType SpineRotationType => talkRotationType;

        public enum EquipInInteraction : byte {
            None,
            AnyWeapon,
            MeleeWeapon,
            RangedWeapon,
            SpecificWeapon,
            SpecificPrefab
        }

        public override bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            return base.AvailableFor(npc, finder)
                   && (equipInInteraction is not EquipInInteraction.SpecificWeapon ||
                       npc.OwnedItemTemplates.Any(t => t == weaponToUse));
        }

        protected override void BeforeDelayStart(NpcElement npc) {
            if (closeEyes) {
                npc.CloseEyes();
            }
        }

        protected override void AfterDelayStart(NpcElement npc) {
            if (equipInInteraction is not EquipInInteraction.None) {
                EquipItem(npc);
            }
        }

        async UniTaskVoid InstantiateItem(NpcElement npc) {
            Transform hand = handToSpawnItem == CastingHand.MainHand ? npc.MainHand : npc.OffHand;
            _itemPrefabToken = new CancellationTokenSource();
            _usedItemPrefab = await PrefabPool.Instantiate(itemToUse, Vector3.zero, Quaternion.identity, hand, Vector3.one, _itemPrefabToken.Token);
        }

        protected override void BeforeDelayExit(NpcElement npc, InteractionStopReason reason) {
            if (closeEyes) {
                bool instant = reason is InteractionStopReason.NPCPresenceDisabled
                    or InteractionStopReason.MySceneUnloading
                    or InteractionStopReason.ComebackFromScene or InteractionStopReason.StoppedIdlingInstant;
                npc.OpenEyes(instant);
            }
        }

        protected override void OnStartTalk() {
            RemoveVFX();
            RemoveSFX();
        }

        protected override void OnEndTalk() {
            _currentLookAtTarget = null;
        }

        public override bool LookAt(NpcElement npc, GroundedPosition grounded, bool lookAtOnlyWithHead) {
            if (_currentLookAtTarget != null && _currentLookAtTarget.Equals(grounded)) {
                return false;
            }

            if (grounded.IsEqualTo(npc)) {
                Log.Important?.Error($"Npc is trying to look at yourself! {npc}", npc?.ParentModel?.Spec);
                return false;
            }
            
            _currentLookAtTarget = grounded;
            _interactingNpc?.ParentModel.Trigger(StoryInteraction.Events.LocationLookAtChanged, new LookAtChangedData(grounded, lookAtOnlyWithHead));

            return true;
        }

        protected override void EnableEffectsFromUpdate() {
            bool vfxSet = interactionVFX?.IsSet ?? false;
            bool statusSet = statusTemplate?.IsSet ?? false;
            bool audioSet = !interactionSFX.IsNull;

            if (vfxSet && !_interactionVFXSpawned) {
                ApplyVFX().Forget();
            }

            if (statusSet && _status == null) {
                ApplyStatus();
            }

            if (audioSet) {
                ApplySFX();
            }
        }

        protected override void ForceEndEffects(NpcElement npc) {
            base.ForceEndEffects(npc);
            RemoveStatus();
            RemoveVFX();
            RemoveSFX();
            ReturnUsedItemPrefab();
            UnequipWeapons(npc);
        }

        async UniTaskVoid ApplyVFX() {
            if (!_interactionVFXSpawned) {
                if (interactionVFXPosition == null) {
                    interactionVFXPosition = transform;
                }

                _interactionVFXSpawned = true;
                _vfxToken = new CancellationTokenSource();
                _vfxInstance = await PrefabPool.Instantiate(interactionVFX, Vector3.zero, Quaternion.identity,
                    interactionVFXPosition,
                    cancellationToken: _vfxToken.Token);
            }
        }

        void RemoveVFX() {
            if (_interactionVFXSpawned) {
                ReturnVFX();
                _interactionVFXSpawned = false;
            }
        }

        void ApplyStatus() {
            if (_status == null) {
                CharacterStatuses statuses = _interactingNpc.Statuses;
                var statusTemplateInstance = statusTemplate.Get<StatusTemplate>();
                _status = statuses.AddStatus(statusTemplateInstance, StatusSourceInfo.FromStatus(statusTemplateInstance),
                    new TimeDuration(float.PositiveInfinity)).newStatus;
            }
        }

        void RemoveStatus() {
            if (_status is { HasBeenDiscarded: false }) {
                _status.Character?.Statuses.RemoveStatus(_status);
            }

            _status = null;
        }

        void ApplySFX() {
            ARFmodEventEmitter eventEmitter = GetComponent<ARFmodEventEmitter>();
            if (eventEmitter == null) {
                eventEmitter = gameObject.AddComponent<ARFmodEventEmitter>();
                //eventEmitter.StopEvent = EmitterGameEvent.ObjectDestroy;
            }

            // if (!eventEmitter.IsPlaying() || eventEmitter.EventReference.Guid != interactionSFX.Guid) {
            //     eventEmitter.PlayNewEventWithPauseTracking(interactionSFX);
            // }
        }

        void RemoveSFX() {
            if (this == null) {
                return;
            }

            ARFmodEventEmitter eventEmitter = GetComponent<ARFmodEventEmitter>();
            if (eventEmitter != null) {
                //eventEmitter.Stop();
            }
        }

        void EquipItem(NpcElement npc) {
            switch (equipInInteraction) {
                case EquipInInteraction.AnyWeapon:
                    EquipWeapon(npc, npc.Inventory.Items.FirstOrDefault(i => i.IsWeapon));
                    break;
                case EquipInInteraction.MeleeWeapon:
                    EquipWeapon(npc, npc.Inventory.Items.FirstOrDefault(i => i.IsMelee));
                    break;
                case EquipInInteraction.RangedWeapon:
                    EquipWeapon(npc, npc.Inventory.Items.FirstOrDefault(i => i.IsRanged));
                    break;
                case EquipInInteraction.SpecificWeapon:
                    if (weaponToUse == null) {
                        Log.Minor?.Error($"Weapon to use is not set {this}", this);
                        break;
                    }

                    EquipWeapon(npc, npc.Inventory.Items.FirstOrDefault(i => i.Template == weaponToUse));
                    break;
                case EquipInInteraction.SpecificPrefab:
                    if (itemToUse is { IsSet: true }) {
                        InstantiateItem(npc).Forget();
                    } else {
                        Log.Minor?.Error($"Item to use is not set {this}", this);
                    }

                    break;
            }
        }

        void EquipWeapon(NpcElement npc, Item item) {
            if (item == null) {
                Log.Minor?.Error($"NPC {npc} doesn't have {weaponToUse} for this interaction {this}", this);
                return;
            }

            if (item.IsEquipped || npc.Inventory.Equip(item)) {
                _usedItem = item;
                EnemyBaseClass enemyBaseClass = npc.ParentModel.TryGetElement<EnemyBaseClass>();
                if (enemyBaseClass == null) {
                    Log.Minor?.Error($"NPC {npc} doesn't have EnemyBaseClass for this interaction {this}", this);
                    return;
                }

                NpcWeaponsHandler npcWeaponsHandler = npc.TryGetElement<NpcWeaponsHandler>();
                if (npcWeaponsHandler == null) {
                    Log.Minor?.Error($"NPC {npc} doesn't have NpcWeaponsHandler for this interaction {this}", this);
                    return;
                }

                npcWeaponsHandler.AttachWeaponToHand(item.Element<ItemEquip>(), equipHandOverride);
            } else {
                Log.Minor?.Error($"NPC {npc} couldn't equip {weaponToUse} for this interaction {this}", this);
            }
        }

        void UnequipWeapons(NpcElement npc) {
            if (equipInInteraction is EquipInInteraction.None or EquipInInteraction.SpecificPrefab) {
                return;
            }

            if (npc is not { HasBeenDiscarded: false } || npc.IsInCombat() || _usedItem is not { HasBeenDiscarded: false, IsEquipped: true }) {
                return;
            }

            npc.Inventory.Unequip(_usedItem);
        }

        void ReturnUsedItemPrefab() {
            _itemPrefabToken?.Cancel();
            _itemPrefabToken = null;
            _usedItemPrefab?.Return();
            _usedItemPrefab = null;
        }

        void ReturnVFX() {
            _vfxToken?.Cancel();
            _vfxToken = null;
            _vfxInstance?.Return();
            _vfxInstance = null;
        }
    }
}