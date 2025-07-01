using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;
using Awaken.Utility.Extensions;
using FMODUnity;
using Newtonsoft.Json;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class LockAction : AbstractLocationAction, IRefreshedByAttachment<LockAttachment>, ILogicReceiverElement {
        public override ushort TypeForSerialization => SavedModels.LockAction;

        [Saved] public bool Locked { get; private set; }
        [Saved] float[] _angles;
        [Saved] LockTolerance _toleranceOverride;

        LockAttachment _spec;
        LockTolerance _tolerance;
        
        LockAtTime LockAtTime => _spec.LockedAtTime;
        EventReference UnlockSound => _spec.unlockSound;
        LockAttachment.KeyLock KeyLock => _spec.keyLock;
        // Logic receiver
        bool UnlockOnStateChanged => _spec.unlockOnStateChanged;
        bool LockOnStateChanged => _spec.lockOnStateChanged;
        public int Complexity { get; private set; }
        public LockTolerance Tolerance {
            get => _toleranceOverride ?? _tolerance;
            private set => _tolerance = value;
        }

        HeroItems HeroItems => World.Only<HeroItems>();
        bool HeroPossessesTool => HeroItems.Items.Any(i => i.HasElement<Lockpick>());
        bool HeroPossessesKey => HeroItems.Items.Any(i => i.Template == KeyLock.ItemTemplate);
        bool WillBeOpenWithKey => KeyLock.use && HeroPossessesKey;
        bool WillTryLockpicking => !KeyLock.use || !WillBeOpenWithKey && HeroCanLockpick;
        bool HeroCanLockpick => !KeyLock.keyOnly && HeroPossessesTool;
        public Type Get3DViewType => _spec.Get3DViewType;
        public IReadOnlyList<float> Angles => _angles;
        protected override InteractRunType RunInteraction => InteractRunType.DontRun;

        public override bool IsIllegal => WillTryLockpicking && Crime.Lockpicking(ParentModel).IsCrime();

        public override InfoFrame ActionFrame => new(string.Empty, false);
        public override InfoFrame InfoFrame1 {
            get {
                if (KeyLock.use) {
                    if (KeyLock.ItemTemplate != null) {
                        var key = HeroItems.Items.FirstOrDefault(i => i.Template == KeyLock.ItemTemplate);
                        if (key != null) {
                            string itemName = key.Quantity > 1
                                ? LocTerms.ItemWithPossessedQuantity.Translate(key.DisplayName, key.Quantity.ToString())
                                : key.DisplayName;
                            string displayText = $"{LocTerms.Unlock.Translate()} ({LocTerms.ItemWillBeUsed.Translate(itemName)})";
                            return new InfoFrame(displayText, true);
                        }
                    }

                    if (KeyLock.keyOnly) {
                        string overrideText = KeyLock.OverrideLockedInfo;
                        if (!overrideText.IsNullOrWhitespace()) {
                            return new InfoFrame(overrideText, false);
                        }
                    }

                    if (KeyLock.ItemTemplate != null) {
                        return new InfoFrame($"{LocTerms.Unlock.Translate()} ({LocTerms.ToolRequired.Translate(KeyLock.ItemTemplate.ItemName)})", false);
                    }

                    if (KeyLock.keyOnly) {
                        return new InfoFrame(LocTerms.BrokenLockInfo.Translate(), false);
                    }
                }
                
                return InfoFrame.Empty;
            }
        }
        
        public override InfoFrame InfoFrame2 {
            get {
                if (!KeyLock.use || (!HeroPossessesKey && !KeyLock.keyOnly)) {
                    var lockpick = HeroItems.Items.FirstOrDefault(i => i.HasElement<Lockpick>());
                    return lockpick != null
                               ? new InfoFrame(LocTerms.ItemWithPossessedQuantity.Translate(lockpick.DisplayName, lockpick.Quantity.ToString()), true)
                               : new InfoFrame(LocTerms.ItemWithPossessedQuantity.Translate(LocTerms.Lockpick.Translate(), "0"), false);
                }
                return InfoFrame.Empty;
            }
        }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        LockAction() { }

        public LockAction(LockAttachment spec) {
            Locked = spec.StartLocked;
            if (spec.Randomized) {
                _angles = new float[spec.Complexity];
                for (int i = 0; i < spec.Complexity; i++) {
                    _angles[i] = Random.Range(0, 180);
                }
            }
        }

        public void InitFromAttachment(LockAttachment spec, bool isRestored) {
            _spec = spec;
            if (!spec.Randomized) {
                _angles = spec.Angles.ToArray();
            }
            Tolerance = spec.Tolerance;
            Complexity = spec.Complexity - 1; // To index
        }

        protected override void OnFullyInitialized() {
            if (!Disabled && Locked) {
                OnEnabled();
            }

            if (LockAtTime == LockAtTime.OnlyAtNight) {
                var time = World.Only<GameRealTime>();
                time.ListenTo(GameRealTime.Events.DayBegan, _ => Unlock(true), this);
                time.ListenTo(GameRealTime.Events.NightBegan, Lock, this);
                if (time.WeatherTime.IsNight) {
                    Lock();
                } else {
                    Unlock(true);
                }
            } else if (LockAtTime == LockAtTime.OnlyAtDay) {
                var time = World.Only<GameRealTime>();
                time.ListenTo(GameRealTime.Events.DayBegan, Lock, this);
                time.ListenTo(GameRealTime.Events.NightBegan, _ => Unlock(true), this);
                if (time.WeatherTime.IsNight) {
                    Unlock(true);
                } else {
                    Lock();
                }
            }
        }

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (ParentModel.HasElement<LockpickingInteraction>()) {
                return;
            }

            if (hero.HasElement<ToolboxOverridesMarker>() || WillBeOpenWithKey) {
                Unlock();
            } else if (HeroCanLockpick) {
                CommitCrime.Lockpicking(ParentModel);
                var lockpicking = new LockpickingInteraction(this);
                ParentModel.AddElement(lockpicking);
                ParentModel.ListenTo(LockpickingInteraction.Events.Unlocked, _ => Unlock(), lockpicking);
            }
        }

        protected override void OnEnabled() {
            // Disable all other interactions
            ParentModel.Elements<AbstractLocationAction>().Where(a => a != this).ForEach(a => a.DisableAction());
        }

        protected override void OnDisabled() {
            // This could cause some awkward interactions/infinite loop if two Actions both manipulate other action Enabled/Disabled not sure what a good solution would be.
            Unlock();
        }

        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return Locked ? base.GetAvailability(hero, interactable) : ActionAvailability.Disabled;
        }

        void Unlock(bool isAutoUnlock = false) {
            ParentModel.Elements<AbstractLocationAction>().Where(a => a != this).ForEach(a => a.EnableAction());
            ParentModel.TryGetElement<LockpickingInteraction>()?.Discard();
            Locked = false;
            if (!UnlockSound.IsNull && !isAutoUnlock) {
                //RuntimeManager.PlayOneShotAttached(UnlockSound, ParentModel.MainView.gameObject, ParentModel.MainView);
            }
            ParentModel.TriggerChange();
        }

        public void Lock() {
            Locked = true;
            if (ParentModel.HasElement<LockpickingInteraction>() || Disabled) return;
            OnEnabled();
        }

        public void DecreaseDifficulty(int amount) {
            bool foundDefault = false;
            
            foreach (LockTolerance tolerance in RichEnum.AllValuesOfType<LockTolerance>().OrderByDescending(t => t.index)) {
                if (!foundDefault) {
                    if (tolerance == _tolerance) {
                        foundDefault = true;
                    }
                    continue;
                }

                amount--;
                if (amount == 0) {
                    _toleranceOverride = tolerance;
                    break;
                }
            }
            
            // If we are at the lowest difficulty and need a further decrease, unlock the lock
            if (amount > 0 && Locked) {
                Unlock();
            }
        }

        public void OnLogicReceiverStateChanged(bool state) {
            if (Locked) {
                if (UnlockOnStateChanged) {
                    Unlock();
                }
            } else if (LockOnStateChanged) {
                Lock();
            }
        }
    }
}