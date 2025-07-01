using System;
using Awaken.TG.Main.Heroes.Items.Weapons;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    [RequireComponent(typeof(ItemTemplate))]
    [AttachesTo(typeof(ItemTemplate), AttachmentCategory.Common, "For equippable items with stats requirements.")]
    public class ItemStatsRequirementsAttachment : MonoBehaviour, IAttachmentSpec {
        [Range(0,100)] public int strengthRequired;
        [Range(0,100)] public int dexterityRequired;
        [Range(0,100)] public int spiritualityRequired;
        [Range(0,100)] public int perceptionRequired;
        [Range(0,100)] public int enduranceRequired;
        [Range(0,100)] public int practicalityRequired;
        
        public Element SpawnElement() {
            return new ItemStatsRequirements();
        }

        public bool IsMine(Element element) => element is ItemStatsRequirements;
        
        // === Editor tools
#if UNITY_EDITOR
        bool CanUseEditorTools => Application.isPlaying && Hero.Current;
        
        [PropertySpace]
        [Button("Give hero required stats", ButtonSizes.Medium)]
        [ShowIf(nameof(CanUseEditorTools))]
        void GiveHeroRequiredStats() => ApplyToAllRequiredStats(IncreaseStatToValue);
        void IncreaseStatToValue(Stat stat, float requiredValue) {
            stat.SetAtLeastTo(requiredValue, 0.0f);
        }

        [PropertySpace]
        [Button("Set hero stats to required", ButtonSizes.Medium)]
        [ShowIf(nameof(CanUseEditorTools))]
        void SetHeroStatsToRequired() => ApplyToAllRequiredStats(SetStatToValueOrInnate);
        void SetStatToValueOrInnate(Stat stat, float requiredValue) {
            if (stat.Type is HeroRPGStatType rpgStatType) {
                float innateValue = GameConstants.Get.RPGStatParamsByType[rpgStatType].InnateStatLevel;
                stat.SetTo(math.max(innateValue, requiredValue));
            }
        }

        void ApplyToAllRequiredStats(Action<Stat, float> modifyAction) {
            HeroRPGStats heroRPGStats = Hero.Current.HeroRPGStats;
            modifyAction(heroRPGStats.Strength, strengthRequired);
            modifyAction(heroRPGStats.Dexterity, dexterityRequired);
            modifyAction(heroRPGStats.Spirituality, spiritualityRequired);
            modifyAction(heroRPGStats.Perception, perceptionRequired);
            modifyAction(heroRPGStats.Endurance, enduranceRequired);
            modifyAction(heroRPGStats.Practicality, practicalityRequired);
        }
#endif
        
    }
}