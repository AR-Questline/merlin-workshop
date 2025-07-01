using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Fights.Utils;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.VFX;

namespace Awaken.TG.Main.Heroes.Items.Buffs {
    public partial class AppliedItemBuff : Element<Item>, IItemSkillOwner, IWithDuration {
        public override ushort TypeForSerialization => SavedModels.AppliedItemBuff;

        const string VFXEffectParam = "EffectValue";
        
        [Saved] ItemTemplate _buffTemplate;
        [Saved] ShareableARAssetReference _vfx;
        GameObject _vfxInstance;
        IEventListener _vfxListener;
        bool _vfxUpdateCompleted;
        VisualEffect _visualEffect;
        ItemBuffFakeStatus _status;
        
        VisualEffect VisualEffect => _visualEffect ??= _vfxInstance.GetComponent<VisualEffect>();

        public string DisplayName { get; private set; }
        public int SecondsLeft => (int)(TryGetElement<TimeDuration>()?.TimeLeft ?? 0);
        public ICharacter Character => Item.Owner?.Character;
        public ItemActionType Type => ItemActionType.Equip;
        public Item Item => ParentModel;
        public IEnumerable<Skill> Skills => Elements<Skill>().GetManagedEnumerator();
        public int PerformCount { get; set; }
        public IModel TimeModel => ParentModel;
        public ItemTemplate Template => _buffTemplate;

        // === Events
        public new static class Events {
            public static readonly Event<IItemOwner, float> WeaponBuffVFXUpdate = new(nameof(WeaponBuffVFXUpdate));
            public static readonly Event<IItemOwner, bool> WeaponBuffVFXUpdateCompleted = new(nameof(WeaponBuffVFXUpdateCompleted));
        }
        
        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        AppliedItemBuff() {}

        public AppliedItemBuff(ItemBuffApplier applier) {
            _buffTemplate = applier.ParentModel.Template;
            _vfx = new ShareableARAssetReference(applier.VFX.Get());
            AddElement(new TimeDuration(applier.Duration));
        }
        
        public AppliedItemBuff(ItemTemplate template, ShareableARAssetReference vfx, int duration) {
            _buffTemplate = template;
            _vfx = vfx;
            AddElement(new TimeDuration(duration));
        }
        
        protected override void OnInitialize() {
            DisplayName = _buffTemplate.ItemName;
            if (ParentModel.View<CharacterHandBase>() != null) {
                ApplyVFX(false);
            }
            if (Item.IsEquipped) {
                ApplyStatus();
            }
            this.ListenTo(Model.Events.AfterFullyInitialized, InitSkills, this);
            InitVisualListeners();
        }

        protected override void OnRestore() {
            DisplayName = _buffTemplate.ItemName;
            if (Item.View<CharacterHandBase>() != null) {
                ApplyVFX(true);
            }
            if (Item.IsEquipped) {
                ApplyStatus();
            }
            InitVisualListeners();
        }

        protected override void OnFullyInitialized() {
            Element<IDuration>().ListenTo(Model.Events.AfterDiscarded, () => {
                if (!HasBeenDiscarded) {
                    Discard();
                }
            }, this);
        }

        void InitVisualListeners() {
            Item.ListenTo(Item.Events.Equipped, OnItemEquip, this);
            Item.ListenTo(Item.Events.Unequipped, OnItemUnequip, this);
            
            ParentModel.ListenTo(CharacterHandBase.Events.WeaponAttached, OnWeaponAttached, this);
            ParentModel.ListenTo(CharacterHandBase.Events.WeaponDestroyed, OnWeaponDestroyed, this);
            ParentModel.Owner?.ListenTo(Events.WeaponBuffVFXUpdateCompleted, _ => _vfxUpdateCompleted = true, this);
        }
        
        void InitSkills() {
            foreach (var skill in Skills) {
                skill.Learn();
            }
            
            if (Item.IsEquipped) {
                foreach (var skill in Skills) {
                    skill.Equip();
                }
            }
        }

        public void Submit() { }
        public void AfterPerformed() { }
        public void Perform() { }
        public void Cancel() { }

        // === Weapon LifeCycle
        void OnWeaponAttached() {
            if (_visualEffect != null) {
                return;
            }
            ApplyVFX(true);
            ApplyStatus();
        }

        void OnWeaponDestroyed() {
            DetachVFX();
        }

        void OnItemEquip() {
            ApplyStatus();
        }

        void OnItemUnequip() {
            RemoveStatus();
        }
        
        // === Discarding
        protected override void OnDiscard(bool fromDomainDrop) {
            DetachVFX();
            RemoveStatus();
        }

        // === Helpers
        void ApplyVFX(bool instant) {
            _vfxUpdateCompleted = false;
            Transform vfxParent = ParentModel.MainView != null ? ParentModel.MainView.transform : null;
            if (vfxParent != null && _vfx is {IsSet: true}) {
                LoadVFX(_vfx.Get(), vfxParent.position, vfxParent.rotation, vfxParent, instant);
            }
        }

        void LoadVFX(ARAssetReference vfxRef, Vector3 position, Quaternion rotation, Transform parent, bool instant) {
            vfxRef.LoadAsset<GameObject>().OnComplete(h => {
                if (HasBeenDiscarded || h.Status != AsyncOperationStatus.Succeeded || h.Result == null) {
                    h.Release();
                    return;
                }

                _vfxInstance = Object.Instantiate(h.Result, position, rotation, parent);
                _vfxInstance.transform.localPosition = Vector3.zero;
                _vfxInstance.transform.localRotation = Quaternion.identity;
                _vfxInstance.AddComponent<OnDestroyReleaseAsset>().Init(vfxRef);
                if (instant || _vfxUpdateCompleted) {
                    UpdateVFXEffectValue(1);
                    return;
                }
                UpdateVFXEffectValue(0);
                _vfxListener = ParentModel.Owner?.ListenTo(Events.WeaponBuffVFXUpdate, UpdateVFXEffectValue, this);
            });
        }

        void UpdateVFXEffectValue(float value) {
            VisualEffect.SetFloat(VFXEffectParam, Mathf.Clamp01(value));
            if (value >= 1) {
                DetachVFXListener();
            }
        }

        void DetachVFX() {
            if (_vfxInstance != null) {
                Object.Destroy(_vfxInstance);
                _vfxInstance = null;
            }
            _visualEffect = null;
            DetachVFXListener();
        }

        void DetachVFXListener() {
            if (_vfxListener != null) {
                World.EventSystem.RemoveListener(_vfxListener);
                _vfxListener = null;
            }
        }

        void ApplyStatus() {
            if (_status is not { HasBeenDiscarded: false }) {
                _status = new ItemBuffFakeStatus(this, _buffTemplate);
                var originalDuration = TryGetElement<TimeDuration>();
                if (originalDuration == null) {
                    Log.Important?.Error($"Applied Buff applied on {LogUtils.GetDebugName(Item)} has no duration, not applying status");
                    return;
                }
                Character.Statuses.AddNewStatus(_status, originalDuration);
                // Status Duration starts with Original Time not Time Left, so we need to reduce the time for correct visualization
                if (_status.TryGetElement<StatusDuration>()?.Duration is TimeDuration timeDuration && timeDuration.TimeLeft != originalDuration?.TimeLeft) {
                    timeDuration.ReduceTimeSeconds(timeDuration.TimeLeft - originalDuration.TimeLeft);
                }
            }
        }

        void RemoveStatus() {
            _status?.Discard();
            _status = null;
        }
    }
}