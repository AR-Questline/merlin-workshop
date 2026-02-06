using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Character.Features;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Development.SarrasPowers;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Development.WyrdPowers;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Heroes.Items.Attachments.Interfaces;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving.Models;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.Debugging;
using Unity.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Heroes {
    public partial class CachedHeroData : Model, IItemOwner {
        public sealed override bool IsNotSaved => true;

        public override Domain DefaultDomain => Domain.Gameplay;

        byte[] _bodyFeatures;
        AliveAudioContainer _audioContainer;

        int[][] _heroTalents;
        TalentTreeBranchType _selectedSarrasTalentBranch;
        WyrdSoulFragmentType[] _wyrdSoulFragments;
        
        byte[] _heroStats;
        byte[] _heroMultStats;
        byte[] _heroRPGStats;
        byte[] _characterStats;
        byte[] _aliveStats;
        byte[] _statusStats;
        byte[] _proficiencyStats;
        byte[] _merchantStats;

        Dictionary<EquipmentSlotType, WeakModelRef<Item>> _cachedEquipment;
        CachedLoadout[] _cachedLoadouts;
        
        SaveBlocker _blocker;
        
        public IInventory Inventory => null;
        public ICharacter Character => null;
        public IEquipTarget EquipTarget => null;

        protected override void OnInitialize() {
            _blocker = World.Add(new SaveBlocker(this));
            StatTweak.Override(Hero.Current.HeroMultStats.ExpMultiplier, 0, parentModel: this).MarkedNotSaved = true;
        }

        public void StashVisuals(Hero hero) {
            StashModel(hero.BodyFeatures(), out _bodyFeatures);
            _audioContainer = hero.AliveAudio?.GetContainer(false);
        }

        public void StashDevelopment(Hero hero) {
            var sarrasTreeBranches = hero.Development.SarrasHeroTreeBranches;
            _selectedSarrasTalentBranch = sarrasTreeBranches.IsUnlocked ? sarrasTreeBranches.CurrentlySelected : TalentTreeBranchType.None;
            StashTalents(hero.Talents, out _heroTalents);

            StashWyrdSoulFragments(hero.Development.WyrdSoulFragments, out _wyrdSoulFragments);
                
            StashModel(hero.AliveStats, out _aliveStats);
            StashModel(hero.CharacterStats, out _characterStats);
            StashModel(hero.StatusStats, out _statusStats);
            StashModel(hero.HeroMultStats, out _heroMultStats);
            StashModel(hero.HeroStats, out _heroStats);
            StashModel(hero.MerchantStats, out _merchantStats);
            StashModel(hero.HeroRPGStats, out _heroRPGStats);
            StashModel(hero.ProficiencyStats, out _proficiencyStats);
        }

        public void StashItems(Hero hero) {
            bool equipSoundsWereMuted = hero.MuteEquips;
            hero.MuteEquips = true;
            _cachedEquipment = new Dictionary<EquipmentSlotType, WeakModelRef<Item>>();
            foreach (var eqSlot in EquipmentSlotType.All) {
                if (eqSlot == EquipmentSlotType.FoodQuickSlot) {
                    // We can't cache food quickslot because it's handled automatically and can lead to race conditions.
                    continue;
                }
                if (eqSlot == EquipmentSlotType.MainHand || eqSlot == EquipmentSlotType.OffHand || eqSlot == EquipmentSlotType.Quiver) {
                    // We can't cache weapons because it's handled by loadouts
                    continue;
                }
                var item = hero.HeroItems.EquippedItem(eqSlot);
                if (item != null) {
                    _cachedEquipment[eqSlot] = item;
                }
            }

            _cachedLoadouts = new CachedLoadout[hero.HeroItems.Loadouts.Count()];
            for (uint i = 0; i < _cachedLoadouts.Length; i++) {
                var loadout = hero.HeroItems.Loadouts.At(i);
                _cachedLoadouts[i] = new CachedLoadout(hero.HeroItems.Loadouts.At(i));
                loadout.InternalAssignItem(EquipmentSlotType.MainHand, null);
                loadout.InternalAssignItem(EquipmentSlotType.OffHand, null);
                loadout.InternalAssignItem(EquipmentSlotType.Quiver, null);
            }
            
            foreach (var slot in EquipmentSlotType.All) {
                hero.Inventory.Unequip(slot);
            }
            
            foreach (var item in hero.Inventory.Items.ToArray()) {
                if (item is { IsFists: false }) {
                    hero.Inventory.Remove(item, false);
                    RelatedList(IItemOwner.Relations.Owns).Add(item);
                }
            }
            hero.MuteEquips = equipSoundsWereMuted;
        }
        
        void StashModel(Model model, out byte[] data) {
            var context = new SaveWriterContext {
                domain = Domain.Gameplay,
            };
            using (var stream = new NativeMemoryStream(64, Allocator.Temp)) {
                using (var saveWriter = new SaveWriter(stream, context)) {
                    saveWriter.WriteStart();
                    model.Serialize(saveWriter);
                    saveWriter.WriteEnd();
                }
                data = stream.ToArray();
            }
        }
        
        void StashTalents(HeroTalents from, out int[][] stash) {
            var fromTables = from.Elements<TalentTable>().ToArraySlow();
            stash = new int[fromTables.Length][];
            for (int i = 0; i < fromTables.Length; i++) {
                stash[i] = new int[fromTables[i].talents.Count];
                for (int j = 0; j < stash[i].Length; j++) {
                    stash[i][j] = fromTables[i].talents[j].Level;
                }
            }
        }

        void StashWyrdSoulFragments(WyrdSoulFragments wyrdSoulFragments, out WyrdSoulFragmentType[] fragments) {
            if (wyrdSoulFragments.UnlockedFragmentsCount == 0) {
                fragments = Array.Empty<WyrdSoulFragmentType>();
                return;
            }
            fragments = wyrdSoulFragments.UnlockedFragments.ToArray();
        }

        public void RestoreVisuals(Hero hero) {
            var bodyFeatures = new BodyFeatures();
            RestoreModel(bodyFeatures, _bodyFeatures);
            hero.BodyFeatures().MoveFrom(bodyFeatures);
            if (_audioContainer != null) {
                hero.AliveAudio?.Discard();
                hero.AddElement(new HeroAliveAudio(_audioContainer));
                Hero.UnloadGenderSoundBanks();
                Hero.LoadGenderSoundBanks(bodyFeatures.Gender);
            }
        }
        
        public void RestoreDevelopment(Hero hero) {
            RestoreModel(hero.ProficiencyStats, _proficiencyStats);
            hero.ProficiencyStats.RecalculateAllStats(false);
            RestoreModel(hero.HeroRPGStats, _heroRPGStats);
            hero.HeroRPGStats.RecalculateAllStats(false);
            RestoreModel(hero.MerchantStats, _merchantStats);
            hero.MerchantStats.RecalculateAllStats(false);
            RestoreModel(hero.HeroStats, _heroStats);
            hero.HeroStats.RecalculateAllStats(false);
            RestoreModel(hero.HeroMultStats, _heroMultStats);
            hero.HeroMultStats.RecalculateAllStats(false);
            RestoreModel(hero.StatusStats, _statusStats);
            hero.StatusStats.RecalculateAllStats(false);
            RestoreModel(hero.CharacterStats, _characterStats);
            hero.CharacterStats.RecalculateAllStats(0,0,false);
            RestoreModel(hero.AliveStats, _aliveStats);
            hero.AliveStats.RecalculateAllStats(0,0,false);
            
            // Updates item stat requirements
            hero.Trigger(StatType.Events.StatOfTypeChanged<HeroRPGStatType>(), hero.HeroRPGStats.Strength);
            
            RestoreWyrdSoulFragments(hero.Development.WyrdSoulFragments, _wyrdSoulFragments);
            
            RestoreTalents(hero.Talents, _heroTalents, _selectedSarrasTalentBranch);
            hero.Development.SarrasHeroTreeBranches.SelectTalentTreeBranch(_selectedSarrasTalentBranch);
        }

        public void RestoreItems(Hero hero) {
            bool equipSoundsWereMuted = hero.MuteEquips;
            hero.MuteEquips = true;
            foreach (var item in RelatedList(IItemOwner.Relations.Owns).ToArray()) {
                RelatedList(IItemOwner.Relations.Owns).Remove(item);
                hero.Inventory.Add(item);
            }

            for (uint i = 0; i < _cachedLoadouts.Length - 1; i++) {
                var loadout = hero.HeroItems.Loadouts.At(i);
                loadout.InternalAssignItem(EquipmentSlotType.MainHand, _cachedLoadouts[i].primary);
                loadout.InternalAssignItem(EquipmentSlotType.OffHand, _cachedLoadouts[i].secondary);
                loadout.InternalAssignItem(EquipmentSlotType.Quiver, _cachedLoadouts[i].quiver);
                if (_cachedLoadouts[i].isEquipped) {
                    loadout.UnequipAll();
                    if (hero.HeroItems.CurrentLoadoutIndex == (int)i) {
                        hero.HeroItems.ActivateLoadout((int)((i + 1) % HeroLoadout.Count), false);
                    }
                    hero.HeroItems.ActivateLoadout((int)i, false);
                }
            }
            
            foreach (var eqSlot in _cachedEquipment) {
                if (eqSlot.Value.TryGet(out var item)) {
                    try {
                        hero.HeroItems.Equip(item, eqSlot.Key);
                    } catch (Exception e) {
                        Log.Critical?.Error($"Error below while equipping cached hero equipment. {LogUtils.GetDebugName(item)} into slot {eqSlot.Key}");
                        Debug.LogException(e);
                        throw;
                    }
                }
            }

            if (hero.HeroItems.EquippedItem(EquipmentSlotType.FoodQuickSlot) != null) {
                // Unequip food slots recalculates best food again;
                hero.HeroItems.Unequip(EquipmentSlotType.FoodQuickSlot);
            }

            hero.MuteEquips = equipSoundsWereMuted;
        }

        void RestoreModel(Model model, byte[] data) {
            var context = new SaveReaderContext {
                deserializedModels = new Dictionary<string, Model>(1024),
            };
            
            using (var stream = new MemoryStream(data)) {
                using (var saveReader = new SaveReader(stream, context)) {
                    saveReader.ReadStart();
                    while (saveReader.TryReadName(out var name)) {
                        model.Deserialize(name, saveReader);
                        saveReader.ReadToSeparator();
                    }
                }
            }
            model.AfterDeserialize();
        }

        void RestoreWyrdSoulFragments(WyrdSoulFragments wyrdSoulFragments, WyrdSoulFragmentType[] fragments) {
            wyrdSoulFragments.LockAll();
            foreach (var fragment in fragments) {
                if (fragment != WyrdSoulFragmentType.Baseline) {
                    wyrdSoulFragments.Unlock(fragment);
                }
            }
        }

        
        void RestoreTalents(HeroTalents to, int[][] stash, TalentTreeBranchType selectedSarrasBranch) {
            var toTables = to.Elements<TalentTable>().ToArraySlow();
            for (int i = 0; i < stash.Length; i++) {
                toTables[i].Reset(withRefund: false);
                for (int j = 0; j < stash[i].Length; j++) {
                    // Give points so we can spend them
                    toTables[i].talents[j].CurrencyStat.IncreaseBy(stash[i][j]);
                    for (int k = 0; k < stash[i][j]; k++) {
                        toTables[i].talents[j].AcquireNextTemporaryLevel();
                    }
                    toTables[i].talents[j].ApplyTemporaryLevels();
                    if (stash[i][j] > 0) {
                        var sarrasBranchType = toTables[i].talents[j].TalentTreeBranchType.ToSarrasTreeBranchType();
                        if (sarrasBranchType != TalentTreeBranchType.None) {
                            if (sarrasBranchType != selectedSarrasBranch) {
                                toTables[i].talents[j].RemoveCurrentSkills();
                            } else {
                                toTables[i].talents[j].RefreshSkills();
                            }
                        }
                    }
                }
            }
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            if (!fromDomainDrop) {
                if (_blocker is { HasBeenDiscarded: false }) {
                    _blocker.Discard();
                    _blocker = null;
                }
            }
        }

        [Serializable]
        public partial struct CachedLoadout {
            public WeakModelRef<Item> primary;
            public WeakModelRef<Item> secondary;
            public WeakModelRef<Item> quiver;
            public bool isEquipped;
            
            public CachedLoadout(HeroLoadout loadout) {
                primary = loadout.PrimaryItem;
                secondary = loadout.PrimaryItem is { IsTwoHanded: false, IsShortBow: false, IsMediumBow: false, IsHeavyBow: false }
                    ? loadout.SecondaryItem
                    : loadout.PrimaryItem;
                quiver = loadout.IsRanged
                    ? loadout.SecondaryItem
                    : null;
                isEquipped = loadout.IsEquipped;
            }
        }
    }
}