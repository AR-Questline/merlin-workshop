using System;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Development.Talents {
    public sealed partial class Talent : Element<TalentTable>, ISkillOwner {
        public override ushort TypeForSerialization => SavedModels.Talent;

        [Saved] public TalentTemplate Template { get; private set; }
        [Saved] public TalentTemplate Parent { get; private set; }
        [Saved(0)] public int Level { get; private set; }
        
        public int EstimatedLevel => Level + _levelToAdd;
        public string CurrentLevelDescription => Template.GetLevel(EstimatedLevel).Description(this, EstimatedLevel);
        public string NextLevelDescription => Template.GetLevel(NextLevel).Description(this, NextLevel);
        public TooltipConstructor TalentKeywords => Template.KeywordDescription(this, EstimatedLevel, NextLevel);
        
        public string TalentName => DebugProjectNames.Basic ? Template.name : Template.Name;
        public int MaxLevel => Template.MaxLevel;
        public bool IsUpgraded => EstimatedLevel > 0;
        public bool IsFirstLevelOrNone => EstimatedLevel <= 1;

        public int RequiredTreeLevelToUnlock => Template.RequiredTreeLevelToUnlock;
        public bool IsLockedByParentTalent => Parent != null && Table.talents.Any(talent => talent.Template == Parent && talent.EstimatedLevel <= 0);
        public bool CanBeReset => Table.MinTreeLevel - (IsFirstLevelOrNone ? RequiredTreeLevelToUnlock : 0) < Table.CurrentTreeLevel && IsUpgraded && _levelToAdd > 0;
        public bool CanBeUpgraded => CanAcquireNextLevel(out _);
        public bool IsMeetRequirements => RequiredTreeLevelToUnlock <= Table.CurrentTreeLevel;
        public bool MaxLevelReached => EstimatedLevel >= MaxLevel;
        public bool WasChanged => EstimatedLevel != Level;
        
        TalentTable Table => ParentModel;
        Hero Hero => Table.Hero;
        Stat CurrencyStat => Hero.Current.Stat(Table.TreeTemplate.CurrencyStatType);
        
        ICharacter ISkillOwner.Character => Hero;
        int NextLevel => EstimatedLevel + 1 <= MaxLevel ? EstimatedLevel + 1 : MaxLevel;
        
        int _levelToAdd;

        public new static class Events {
            /// <summary> Internal change of Talent </summary>
            public static readonly Event<Talent, Talent> TalentChanged = new(nameof(TalentChanged));
            public static readonly Event<Talent, ChangeData> TalentConfirmed = new(nameof(TalentConfirmed));
        }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] Talent() { }
        
        public Talent(TalentTemplate template) {
            Template = template;
        }
        
        public Talent(TalentTemplate template, TalentTemplate parent) {
            Template = template;
            Parent = parent;
        }

        protected override void OnRestore() {
            SkillInitialization.MarkForManualCustomRestore(this);
            LoadSave.Get.LoadSystem.AfterGameRestored(() => {
                int diff = Level - Template.MaxLevel;
                for (int i = 0; i < diff; i++) {
                    CurrencyStat.IncreaseBy(1);
                    Level--;
                }
                var references = Template.GetLevel(Level).Skills;
                SkillInitialization.ManualCustomRestore(this, references, SkillState.Learned);
            });
            base.OnRestore();
        }

        public void CheckTalentTree() {
            bool found = ParentModel.TreeTemplate.Pattern.TalentNodes.Any(talentNode => talentNode.Talent == Template);

            if (!found) {
                Log.Important?.Error("Talent is not present in its saved ParentTable (see talent)", Template);
                Log.Important?.Error("Talent is not present in its saved ParentTable (see table)", Table.TreeTemplate);
            }
        }

        public bool CanAcquireNextLevel(out AcquiringProblem problem) {
            if (MaxLevelReached) {
                problem = AcquiringProblem.MaxLevelReached;
                return false;
            }

            if (CurrencyStat <= 0) {
                problem = AcquiringProblem.NotEnoughTalentPoints;
                return false;
            }
            
            if (IsLockedByParentTalent) {
                problem = AcquiringProblem.ParentLocked;
                return false;
            }
            
            if (RequiredTreeLevelToUnlock > Table.CurrentTreeLevel) {
                problem = AcquiringProblem.TooLowTreeLevel;
                return false;
            }

            problem = AcquiringProblem.None;
            return true;
        }

        public bool AcquireNextTemporaryLevel() {
            if (!CanAcquireNextLevel(out var problem)) {
                Log.Important?.Error(problem switch {
                    AcquiringProblem.MaxLevelReached => "Trying to acquire talent level greater than max level",
                    AcquiringProblem.NotEnoughTalentPoints => "Trying to acquire talent when no talent points",
                    AcquiringProblem.RowNotAccessible => "Trying to acquire talent from not accessible row",
                    AcquiringProblem.ParentLocked => "Trying to acquire talent when parent talent is locked",
                    AcquiringProblem.TooLowTreeLevel => "Trying to acquire talent when too low tree level",
                    AcquiringProblem.TooLowHeroRPGStat => "Trying to acquire talent when too low hero stat level",
                    _ => throw new ArgumentOutOfRangeException(nameof(problem), problem, null)
                }, Template);
                return false;
            }

            CurrencyStat.DecreaseBy(1);
            Table.PointsSpent++;
            _levelToAdd++;

            this.Trigger(Events.TalentChanged, this);
            return true;
        }

        void RefreshSkills() {
            RemoveCurrentSkills();
            foreach (var reference in Template.GetLevel(Level).Skills) {
                AddElement(reference.CreateSkill()).Learn();
            }
        }

        public void ApplyTemporaryLevels() {
            if (_levelToAdd == 0) {
                return;
            }
            var talentChangedData = new ChangeData(this, _levelToAdd);
            Level += _levelToAdd;
            _levelToAdd = 0;
            RefreshSkills();
            this.Trigger(Events.TalentConfirmed, talentChangedData);
        }
        
        public void DecrementTemporaryLevel() {
            if (_levelToAdd <= 0) return;
            
            CurrencyStat.IncreaseBy(1);
            Table.PointsSpent--;
            _levelToAdd--;

            this.Trigger(Events.TalentChanged, this);
        }

        public void ClearTemporaryPoints() {
            if (_levelToAdd <= 0) return;
            
            CurrencyStat.IncreaseBy(_levelToAdd);
            Table.PointsSpent -= _levelToAdd;
            _levelToAdd = 0;
            
            this.Trigger(Events.TalentChanged, this);
        }
        
        public void Reset() {
            if (Level <= 0) return;
            
            CurrencyStat.IncreaseBy(Level);
            Table.PointsSpent -= Level;
            Level = 0;
            
            RemoveCurrentSkills();
            this.Trigger(Events.TalentChanged, this);
        }

        void RemoveCurrentSkills() {
            RemoveElementsOfType<Skill>();
        }

        public enum AcquiringProblem {
            None,
            MaxLevelReached,
            NotEnoughTalentPoints,
            RowNotAccessible,
            ParentLocked,
            TooLowTreeLevel,
            TooLowHeroRPGStat,
        }

        public struct ChangeData {
            public Talent talent;
            public int levelGain;
            
            public ChangeData(Talent talent, int levelGain) {
                this.talent = talent;
                this.levelGain = levelGain;
            }
        }
    }
}