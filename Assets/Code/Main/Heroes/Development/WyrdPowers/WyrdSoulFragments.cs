using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Development.Talents;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.FancyPanel;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Heroes.Development.WyrdPowers {
    public partial class WyrdSoulFragments : Element<HeroDevelopment>, ISkillOwner {
        public override ushort TypeForSerialization => SavedModels.WyrdSoulFragments;

        [Saved] List<WyrdSoulFragmentType> _unlockedFragments = new();

        [Saved(false)] public bool IsActive { get; private set; }

        public int UnlockedFragmentsCount => _unlockedFragments.Count;
        public IEnumerable<WyrdSoulFragmentType> UnlockedFragments => _unlockedFragments;
        public bool HasAnySkill => !_unlockedFragments.IsNullOrEmpty();
        public ICharacter Character => Hero;
        Hero Hero => ParentModel.ParentModel;
        
        protected override void OnInitialize() {
            Init();
        }
        
        protected override void OnRestore() {
            var state = new SkillState {
                learned = true,
                equipped = IsActive
            };

            var unlockedSkills = CommonReferences.Get.WyrdSoulFragments
                .Where(f => _unlockedFragments.Contains(f.fragmentType))
                .Select(f => f.skillReference);

            SkillInitialization.CustomRestore(this, unlockedSkills, state);
            Init();
        }
        
        void Init() {
            Hero.ListenTo(Stat.Events.StatChangedBy(HeroStatType.WyrdMemoryShards), OnShardsChanged, this);
            Hero.ListenTo(Stat.Events.StatChangedBy(HeroStatType.WyrdWhispers), OnWhispersChanged, this);
            Hero.ListenTo(Stat.Events.StatChangedBy(CurrencyStatType.Cobweb), OnCobwebChanged, Hero);
        }
        
        void OnShardsChanged(Stat.StatChange change) {
            if (!TutorialKeys.IsConsumed(TutKeys.TriggerFirstMemoryShardAcquire)) {
                TutorialMaster.Trigger(TutKeys.TriggerFirstMemoryShardAcquire);
            }

            if (change.value > 0) {
                ParentModel.AddMarkerElement(() => new HeroMemoryShardAvailableMarker());
            }
        }
        
        void OnWhispersChanged(Stat.StatChange change) {
            if (change.value > 0) {
                if (!TutorialKeys.IsConsumed(TutKeys.TriggerFirstWyrdWhisperAcquire)) {
                    var involvement = World.Any<IHeroInvolvement>();
                    if (involvement == null) {
                        TutorialMaster.Trigger(TutKeys.TriggerFirstWyrdWhisperAcquire);
                    } else {
                        involvement.ListenTo(Model.Events.AfterDiscarded, _ => {
                            if (!TutorialKeys.IsConsumed(TutKeys.TriggerFirstWyrdWhisperAcquire)) {
                                TutorialMaster.Trigger(TutKeys.TriggerFirstWyrdWhisperAcquire);
                            }
                        }, this);
                    }
                }
            }
        }
        
        void OnCobwebChanged(Stat.StatChange statChange) {
            if (statChange.value > 0) {
                Hero hero = (Hero) statChange.stat.Owner;
                var bonfire = CommonReferences.Get.Bonfire.ToRuntimeData(hero);
                if (!hero.HeroItems.HasItem(bonfire)) {
                    hero.HeroItems.AddWithoutNotification(new Item(bonfire));
                }
                if (!TutorialKeys.IsConsumed(TutKeys.TriggerSetUpCamp)) {
                    TutorialMaster.Trigger(TutKeys.TriggerSetUpCamp);
                }
            }
        }

        public void Unlock(WyrdSoulFragmentType fragmentType) {
            if (!_unlockedFragments.Contains(WyrdSoulFragmentType.Baseline)) {
                InternalUnlock(WyrdSoulFragmentType.Baseline);
            }
            
            if (!_unlockedFragments.Contains(fragmentType)) {
                InternalUnlock(fragmentType);
                FancyPanelType.LearnedWyrdSkill.Spawn(this);
            }
        }
        
        public void LockAll() {
            foreach (var unlockedFragment in _unlockedFragments) {
                if (GetFlag(unlockedFragment) is { } flagToUnlock) {
                    Services.Get<GameplayMemory>().Context().Set(flagToUnlock, false);
                }
            }
            RemoveElementsOfType<Skill>();
            _unlockedFragments.Clear();
            foreach (var talentTable in Hero.Talents.Elements<TalentTable>()) {
                if (talentTable.TreeTemplate.CurrencyStatType == HeroStatType.WyrdMemoryShards) {
                    talentTable.Reset();
                }
            }
        }

        void InternalUnlock(WyrdSoulFragmentType fragmentType) {
            _unlockedFragments.Add(fragmentType);
            var fragment = CommonReferences.Get.GetWyrdSoulFragment(fragmentType);
            var skill = fragment.skillReference.CreateSkill();
            AddElement(skill);
            skill.Learn();
            if (GetFlag(fragmentType) is { } flagToUnlock) {
                Services.Get<GameplayMemory>().Context().Set(flagToUnlock, true);
            }
            Hero.Trigger(Hero.Events.WyrdSoulFragmentCollected, fragmentType);
        }

        public void ActivatePowers() {
            IsActive = true;
            foreach (var skill in Elements<Skill>()) {
                skill.Equip();
            }
        }

        public void DeactivatePowers() {
            IsActive = false;
            foreach (var skill in Elements<Skill>()) {
                skill.Unequip();
            }
        }
        
        static string GetFlag(WyrdSoulFragmentType fragmentType) {
            switch (fragmentType) {
                case WyrdSoulFragmentType.Prologue:
                    return "Tutorial:Wyrdpower";
                case WyrdSoulFragmentType.Excalibur:
                    return "excalibur:taken";
                case WyrdSoulFragmentType.Shield:
                    return "ArthurShield:Taken";
                case WyrdSoulFragmentType.Helmet:
                    return "arthurCrown:taken";
                default:
                    return null;
            }
        }
    }
}