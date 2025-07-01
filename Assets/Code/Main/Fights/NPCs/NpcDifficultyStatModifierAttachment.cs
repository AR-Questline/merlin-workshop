using System;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using UnityEngine;

namespace Awaken.TG.Main.Fights.NPCs {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.ExtraCustom, "Adjusts NPC stats based on difficulty.")]
    public class NpcDifficultyStatModifierAttachment : MonoBehaviour, IAttachmentSpec {
        
        [Tooltip("If set, NPC will receive strength tweak to match players damage multiplier from difficulty.")]
        [SerializeField] bool cancelOutHeroDamageReceivedTweak;
        
        [Tooltip("A list of difficulty to modifier mappings to apply to NPC in this presence")]
        [SerializeField]
        DifficultyStatModifierInfo[] customModifiers = Array.Empty<DifficultyStatModifierInfo>();

        public IEnumerable<NpcDifficultyStatModifier.StatInfo> GetStatsForDifficulty(Difficulty difficulty) {
            foreach (var modifier in customModifiers) {
                if (modifier.Difficulty == difficulty) {
                    yield return new NpcDifficultyStatModifier.StatInfo(modifier.Stat, modifier.Multiplier);
                }
            }
            
            if (cancelOutHeroDamageReceivedTweak && difficulty.DamageReceived > 0f) {
                float cancelOutMultiplier = 1f / difficulty.DamageReceived;
                
                yield return new NpcDifficultyStatModifier.StatInfo(NpcStatType.MeleeDamage, cancelOutMultiplier);
                yield return new NpcDifficultyStatModifier.StatInfo(NpcStatType.MagicDamage, cancelOutMultiplier);
                yield return new NpcDifficultyStatModifier.StatInfo(NpcStatType.RangedDamage, cancelOutMultiplier);
            }
        }
        
        public Element SpawnElement() => new NpcDifficultyStatModifier();
        public bool IsMine(Element element) => element is NpcDifficultyStatModifier;
        
        [Serializable]
        struct DifficultyStatModifierInfo {
            [SerializeField, RichEnumExtends(typeof(Difficulty))] RichEnumReference difficulty;
            [SerializeField, RichEnumExtends(typeof(StatType))] RichEnumReference stat;
            [SerializeField] float multiplier;
            
            public Difficulty Difficulty => difficulty.EnumAs<Difficulty>();
            public StatType Stat => stat.EnumAs<StatType>();
            public float Multiplier => multiplier;
        }
    }
}