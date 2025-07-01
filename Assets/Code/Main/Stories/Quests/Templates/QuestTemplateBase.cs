using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.General;
using Awaken.TG.Main.General.Caches;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.Stories.Quests.Objectives.Specs;
using Awaken.TG.Main.Stories.Quests.UI;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Quests.Templates {
    public abstract class QuestTemplateBase : Template, INamed, IAttachmentGroup {
        const string TextsGroup = "Texts";
        const string ExpGroup = "Exp";
        const string DebugBox = "Debug";
        const string UsagesGroup = "Usages";
        
        // === Properties
        // -- Common
        [FoldoutGroup(TextsGroup, order: 0), LocStringCategory(Category.Quest)]
        public LocString displayName;
        [FoldoutGroup(TextsGroup), LocStringCategory(Category.Quest)]
        public LocString description;
        [FoldoutGroup(ExpGroup, order: 2), Tooltip("Exp reward for completed quest will be calculated based on target lvl and Xp Gain Range.")]
        public int targetLvl = 1;
        [FoldoutGroup(ExpGroup), Tooltip("You can find multipliers for given ranges in Common References -> Systems.")]
        public StatDefinedRange xpGainRange = StatDefinedRange.Custom;
        [ShowIf("@" + nameof(xpGainRange) + "==" + nameof(StatDefinedRange) + "." + nameof(StatDefinedRange.Custom)), FoldoutGroup(ExpGroup)]
        public float experiencePoints;
        [Tooltip("Disable to hide markers for all objectives of this quest."), PropertyOrder(4)]
        public bool showQuestMarkers = true;
        [SerializeField, TemplateType(typeof(FactionTemplate)), Tooltip("Faction is used for proper quest icon in quest log."), PropertyOrder(4)] 
        TemplateReference relatedFaction;
        [ShowAssetPreview, ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true, AddressableGroup.Quests)]
        public ShareableSpriteReference iconDescriptionReference;

        public FactionTemplate RelatedFaction => relatedFaction.Get<FactionTemplate>();

        [ShowInInspector, HideIf("@" + nameof(xpGainRange) + "==" + nameof(StatDefinedRange) + "." + nameof(StatDefinedRange.Custom))]
        [FoldoutGroup(ExpGroup)]
        public FloatRange CalculatedExpRange => QuestUtils.CalculateXpRange(targetLvl, xpGainRange, experiencePoints);
        
        public PooledList<ObjectiveSpecBase> ObjectiveSpecs {
            get {
                PooledList<ObjectiveSpecBase>.Get(out var results);
                GetComponentsInChildren<ObjectiveSpecBase>(results);
                return results;
            }
        }

        public override PooledList<IAttachmentGroup> GetAttachmentGroups() {
            PooledList<IAttachmentGroup>.Get(out var results);
            results.value.Add(this);
            return results;
        }

        public override IEnumerable<IAttachmentSpec> GetAttachments() => ObjectiveSpecs.value;

        // -- Unique
        public abstract QuestType TypeOfQuest { get; }
        public abstract bool AutoCompleteLeftObjectives { get; }
        public abstract IEnumerable<ObjectiveSpecBase> AutoRunObjectives { get; }
        public abstract bool AutoCompletion { get; }
        public abstract IEnumerable<ObjectiveSpecBase> AutoCompleteAfter { get; }
        // === INamed
        public string DisplayName => displayName;
        
        // === Editor
#if UNITY_EDITOR
        public string Editor_GetNameOfObjectiveSpec(string objectiveGuid) {
            using var objectiveSpecs = ObjectiveSpecs;
            var first = objectiveSpecs.value.FirstOrDefault(p => p.Guid == objectiveGuid);
            if (first) {
                return first.GetName();
            }
            Log.Minor?.Warning($"Couldn't find any objective spec of guid {objectiveGuid}");
            return string.Empty;
        }

        public string Editor_GetGuidOfObjectiveSpec(string objectiveName) {
            using var objectiveSpecs = ObjectiveSpecs;
            var first = objectiveSpecs.value.FirstOrDefault(p => p.GetName() == objectiveName);
            if (first) {
                return first.Guid;
            }
            Log.Minor?.Warning($"Couldn't find any objective spec of name {objectiveName}");
            return string.Empty;
        }
        
        [ShowInInspector, FoldoutGroup(UsagesGroup, order: 8), ListDrawerSettings(DefaultExpandedState = true, IsReadOnly = true)]
        // ReSharper disable once InconsistentNaming
        List<QuestSource> _EDITOR_usages;

        [Button, FoldoutGroup(UsagesGroup)]
        void EDITOR_RefreshUsages() => _EDITOR_usages = QuestCache.Get.FindSourcesFor(this);
        
        [Button, EnableIf(nameof(EDITOR_CanUseContexts)), BoxGroup(DebugBox, order: 10)]
        void EDITOR_TeleportToQuestGiver() {
            EDITOR_QuestTemplateDebug.EDITOR_TeleportToQuestGiver(this);
        }

        [Button, EnableIf(nameof(EDITOR_CanStartQuest)), BoxGroup(DebugBox), PropertyOrder(10)]
        void EDITOR_StartQuest() {
            EDITOR_QuestTemplateDebug.EDITOR_StartQuest(this);
        }

        [ShowIf(nameof(EDITOR_CanUseContexts)), ValueDropdown(nameof(EDITOR_AvailableObjectives)), BoxGroup(DebugBox), PropertyOrder(15), NonSerialized, ShowInInspector]
        // ReSharper disable once InconsistentNaming
        public string _EDITOR_chosenObjective;

        IEnumerable<string> EDITOR_AvailableObjectives {
            get {
                yield return string.Empty;
                using var objectiveSpecs = ObjectiveSpecs;
                foreach (var spec in objectiveSpecs.value) {
                    yield return spec.name;
                }
            }
        }
        
        [Button, ShowIf(nameof(EDITOR_IsSameScene)), BoxGroup(DebugBox), PropertyOrder(16)]
        void EDITOR_TeleportToObjective() {
            EDITOR_QuestTemplateDebug.EDITOR_TeleportToObjective(this);
        }

        [Button, ShowIf(nameof(EDITOR_CanUseContexts)), HideIf(nameof(EDITOR_IsSameScene)), BoxGroup(DebugBox), PropertyOrder(16)]
        [InfoBox("$EDITOR_ChangeSceneMessage")]
        void EDITOR_ChangeScene() {
            EDITOR_QuestTemplateDebug.EDITOR_ChangeScene(this);
        }

        string EDITOR_ChangeSceneMessage() {
            return EDITOR_QuestTemplateDebug.EDITOR_ChangeSceneMessage(this);
        }

        [Button, ShowIf(nameof(EDITOR_CanUseContexts)), BoxGroup(DebugBox), PropertyOrder(17)]
        void EDITOR_StartObjective() {
            EDITOR_QuestTemplateDebug.EDITOR_StartObjective(this);
        }

        [Button, ShowIf(nameof(EDITOR_CanUseContexts)), BoxGroup(DebugBox), PropertyOrder(18)]
        void EDITOR_CompleteObjective() {
            EDITOR_QuestTemplateDebug.EDITOR_TryToCompleteObjective(this);
        }

        bool EDITOR_IsSameScene() {
            return EDITOR_QuestTemplateDebug.EDITOR_IsSameScene(this);
        }

        bool EDITOR_CanUseContexts() {
            return Hero.Current != null;
        }

        bool EDITOR_CanStartQuest() {
            GameplayMemory memory = World.Services?.TryGet<GameplayMemory>();
            if (memory == null) {
                return false;
            }
            return !QuestUtils.AlreadyTaken(memory, new TemplateReference(this));
        }
#endif
    }
}