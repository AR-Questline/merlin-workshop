using System.Collections.Generic;
using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Markers;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Duels {
    public abstract partial class DuelistElement : Element<ICharacter>, IKillPreventionListener {
        const float HpPercentageToLose = 0.1f;
        static readonly List<Model> ModelsToDiscard = new ();
        static FactionTemplate DuelFaction => GameConstants.Get.duelFaction.Get<FactionTemplate>();
        
        public sealed override bool IsNotSaved => true;

        readonly DuelistSettings _settings;
        readonly DuelistsGroup _group;

        public DuelController DuelController => _group.Controller;
        public int GroupId => _group.Id;
        public DuelistSettings Settings => _settings;
        public bool Defeated { get; private set; }
        public bool Victorious { get; private set; }

        protected DuelistElement(DuelistsGroup group, DuelistSettings settings) {
            _group = group;
            _settings = settings;
        }
        
        protected override void OnInitialize() {
            ParentModel.OverrideFaction(DuelFaction, FactionOverrideContext.Duel);

            KillPreventionDispatcher.RegisterListener(ParentModel, this); 
            if (Settings.fightToDeath) {
                InitDeathListener();
            }

            World.EventSystem.ListenTo(EventSelector.AnySource, CharacterStatuses.Events.AddedStatus, this, OnStatusApplied);
            ParentModel.ListenTo(HealthElement.Events.DealingDamage, OnBeforeDealingDamage, this);
            ParentModel.ListenTo(INpcSummon.Events.SummonSpawned, OnSummonAdded, this);
        }

        protected abstract void InitDeathListener();

        public void StartDuel() {
            if (Settings.restoreHealthOnStart) {
                ParentModel.HealthStat.SetToFull();
            }
            OnDuelStarted();
        }

        protected virtual void OnDuelStarted() { }
        
        public void EndDuel() {
            OnDuelEnded();
            if (Settings.restoreHealthOnEnd && ParentModel is { HasBeenDiscarded: false, IsAlive: true, HealthStat: { } hpStat }) {
                hpStat.SetToFull();
            }
            Discard();
        }
        
        protected virtual void OnDuelEnded() { }

        public void Victory() {
            Victorious = true;
            AfterDuelCleanup();
            OnVictory();
        }
        
        protected virtual void OnVictory() { }
        
        public void Defeat(bool forceDefeat = false) {
            if (Defeated) {
                return;
            }

            Defeated = true;
            StopFight();
            OnDefeat(forceDefeat);
            _group.OnDuelistDefeated(this);
        }

        protected virtual void OnDefeat(bool forceDefeat) { }
        
        // Listeners
        
        public bool OnBeforeTakingFinalDamage(HealthElement healthElement, Damage damage) {
            float hpAfterDamage = healthElement.Health.ModifiedValue - damage.Amount;
            float minimumHp = HpPercentageToLose * healthElement.MaxHealth.ModifiedValue;
            if (hpAfterDamage >= minimumHp) {
                return false;
            }

            if (Defeated || (!Settings.fightToDeath && !damage.IsPrimary)) {
                healthElement.Health.SetTo(minimumHp);
                Defeat();
                return true;
            }

            if (Settings.fightToDeath) {
                return false;
            }
            
            //Fake damage to trigger all VFX etc (can't deal precalculated damage because others modifiers can be applied) 
            Damage placeholderDamage = damage; 
            placeholderDamage.RawData.SetToZero();
            DamageParameters placeholderDamageParameters = placeholderDamage.Parameters;
            placeholderDamageParameters.ForceDamage = 0; //Prevent Stagger
            placeholderDamageParameters.PoiseDamage = 0;
            placeholderDamageParameters.IsPrimary = false;
            placeholderDamage.Parameters = placeholderDamageParameters;
            
            healthElement.TakeDamage(placeholderDamage);
            healthElement.Health.SetTo(minimumHp);
            Defeat();
            return true;
        }
        
        void OnStatusApplied(Status status) {
            // Don't apply statuses to characters that are not duelists
            if (status.SourceInfo.SourceCharacter.Get() != ParentModel) {
                return;
            }
            if (status.Character.HasElement<DuelistElement>()) {
                return;
            }
            status.Discard();
        }

        void OnBeforeDealingDamage(HookResult<ICharacter, Damage> hook) {
            // Don't deal damage to characters that are not duelists
            if (hook.Value.Target is ICharacter character && !character.HasElement<DuelistElement>()) {
                hook.Prevent();
                return;
            }
        }

        protected void OnBeforeDeath() {
            Defeat();
            _group.RemoveDuelist(this);
        }
        
        void OnSummonAdded(INpcSummon summon) {
            if (summon.Owner != ParentModel) {
                return;
            }
            summon.ParentModel.OnVisualLoaded(OnSummonVisualLoaded);

            void OnSummonVisualLoaded(NpcElement summonNpc, Transform _) {
                if (DuelController is not {HasBeenDiscarded: false } || Victorious || Defeated) {
                    return;
                }
                DuelController.AddDuelist(summonNpc, GroupId, DuelistSettings.Summon);
            }
        }
        
        // Cleanup
        
        protected virtual void AfterDuelCleanup() {
            StopFight();
            RestoreFaction();
            RemoveAllEffects();
        }

        protected virtual void StopFight() {
            _group.Controller.ClearAntagonismTowards(this, GroupId);
            ParentModel.ForceEndCombat();
        }

        void RestoreFaction() {
            ParentModel.ClearAllMarkers(AntagonismLayer.Duel);
            ParentModel.ResetFactionOverride(FactionOverrideContext.Duel);
        }
        
        void RemoveAllEffects() {
            foreach (var status in World.All<Status>()) {
                if (status.SourceInfo.SourceCharacter.Get() == ParentModel) {
                    if (status.Type == StatusType.Curse || status.Type == StatusType.Sin || status.Type == StatusType.Debuff) {
                        ModelsToDiscard.Add(status);
                    }
                }
            }
            foreach (var location in World.All<ItemBasedLocationMarker>()) {
                if (location.Owner == ParentModel) {
                    ModelsToDiscard.Add(location);
                }
            }
            foreach (var model in ModelsToDiscard) {
                model.Discard();
            }
            ModelsToDiscard.Clear();
        }
        
        // Lifecycle

        protected override void OnDiscard(bool fromDomainDrop) {
            KillPreventionDispatcher.UnregisterListener(ParentModel, this);
            if (fromDomainDrop) {
                return;
            }
            AfterDuelCleanup();
            _group.RemoveDuelist(this);
        }
    }
}
