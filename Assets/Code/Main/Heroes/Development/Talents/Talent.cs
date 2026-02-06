using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.TalentTrees.Subtree;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Memories.FilePrefs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Tooltips;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Development.Talents {
    public sealed partial class Talent : Element<TalentTable>, ISkillOwner {
        public override ushort TypeForSerialization => SavedModels.Talent;

        public static ShareableSpriteReference DefaultIconReference => CommonReferences.Get.DefaultStatusFromTalentIcon;

        [Saved] public TalentTemplate Template { get; private set; }
        [Saved] public TalentTemplate Parent { get; private set; }
        [Saved(0)] public int Level { get; private set; }
        [Saved] public TalentSubtreeType TalentTreeBranchType { get; private set; }
        [Saved] StatType OverrideCurrencyStat { get; set; }

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

        public Stat CurrencyStat => OverrideCurrencyStat != null
            ? Hero.Current.Stat(OverrideCurrencyStat)
            : Hero.Current.Stat(Table.TreeTemplate.CurrencyStatType);
        
        ICharacter ISkillOwner.Character => Hero;
        int NextLevel => EstimatedLevel + 1 <= MaxLevel ? EstimatedLevel + 1 : MaxLevel;
        
        int _levelToAdd;
        bool _markedForDiscard;

        public new static class Events {
            /// <summary> Internal change of Talent </summary>
            public static readonly Event<Talent, Talent> TalentChanged = new(nameof(TalentChanged));
            public static readonly Event<Talent, ChangeData> TalentConfirmed = new(nameof(TalentConfirmed));
        }
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] Talent() { }
        
        public Talent(TalentTreeTemplate.TalentTreeNode node, TalentSubtreeType branchType, StatType currencyStat = null) {
            Template = node.Talent;
            Parent = node.Parent;
            OverrideCurrencyStat = currencyStat;
            TalentTreeBranchType = branchType;
        }

        protected override void OnRestore() {
            SkillInitialization.MarkForManualCustomRestore(this);
            
            if (_markedForDiscard) {
                return;
            }
            
            LoadSave.Get.LoadSystem.AfterGameRestored(() => {
                int diff = Level - Template.MaxLevel;
                for (int i = 0; i < diff; i++) {
                    CurrencyStat.IncreaseBy(1);
                    Level--;
                }
                var references = Template.GetLevel(Level).Skills;
                SkillInitialization.ManualCustomRestore(this, references, SkillState.Learned);
            });

            if (TalentTreeBranchType == null || OverrideCurrencyStat == null) {
                var subtree = Table.TreeTemplate.TreeSubTrees
                    .FirstOrDefault(subtree => subtree.TreeNodes
                        .Any(node => node.Talent == Template));

                if (TalentTreeBranchType == null && subtree != null) {
                    TalentTreeBranchType = subtree.SubtreeType;
                }
                
                if (OverrideCurrencyStat == null && subtree != null) {
                    OverrideCurrencyStat = subtree.CurrencyStatType;
                }
            }
            
            base.OnRestore();
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            if (_markedForDiscard) {
                DiscardAfterOneFrame().Forget();
            }
        }

        public bool CheckTalentTree() {
            bool found = ParentModel.TreeTemplate.TalentNodes.Any(talentNode => talentNode.Talent == Template);
#if UNITY_EDITOR
            if (!found) {
                Log.Important?.Error($"Talent is not present in its saved ParentTable (see talent {Template.GUID} {Template.Name})", Template);
                Log.Important?.Error($"Talent is not present in its saved ParentTable (see table {Table.TreeTemplate.GUID} {Table.TreeTemplate.Name})", Table.TreeTemplate);
            }
#endif
            return found;
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

        public void RefreshSkills() {
            RemoveCurrentSkills();
            ApplyCurrentSkills();
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
        
        public void Reset(bool withRefund = true) {
            if (Level <= 0) return;

            if (withRefund) {
                CurrencyStat.IncreaseBy(Level);
            }
            Table.PointsSpent -= Level;
            Level = 0;
            
            RemoveCurrentSkills();
            this.Trigger(Events.TalentChanged, this);
        }

        public void MarkForDiscard() {
            if (IsFullyInitialized) {
                RemoveTalentAndReturnResources();
                return;
            }
            _markedForDiscard = true;
        }

        public void RemoveCurrentSkills() {
            RemoveElementsOfType<Skill>();
        }
        
        void ApplyCurrentSkills() {
            foreach (var reference in Template.GetLevel(Level).Skills) {
                AddElement(reference.CreateSkill()).Learn();
            }
        }

        void RemoveTalentAndReturnResources() {
            // We cannot assume that Talent field exists here
            Log.Important?.Error($"Talent ({ID}) is discarding and returning points to hero");
            CurrencyStat.IncreaseBy(Level);
            Discard();
        }

        async UniTaskVoid DiscardAfterOneFrame() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;    
            }
            RemoveTalentAndReturnResources();
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