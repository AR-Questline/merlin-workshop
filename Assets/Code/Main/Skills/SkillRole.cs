using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Skills {
    /// <summary>
    /// An enum used to refer to various models taking part in a skill -
    /// the hero representing the main character, the place we're in, etc.
    /// </summary>
    public class SkillRole : RichEnum {

        // === Roles
        [UnityEngine.Scripting.Preserve]
        public static readonly SkillRole
            Hero = new(nameof(Hero), s => Heroes.Hero.Current),
            HeroStats = new(nameof(Hero), s => Heroes.Hero.Current.TryGetElement<HeroStats>()),
            Character = new(nameof(Character), s => s.Owner),
            Merchant = new(nameof(Merchant), s => Heroes.Hero.Current),
            Proficiency = new(nameof(Proficiency), s => Heroes.Hero.Current.ProficiencyStats),
            Item = new(nameof(Item), GetItemFromSkill);
        
        static Item GetItemFromSkill(Skill skill) {
            IItemSkillOwner skillOwner = skill.GenericParentModel as IItemSkillOwner;
            return skillOwner?.Item;
        }

        // === Role properties

        protected delegate IModel Getter(Skill s);
        readonly Getter _getter;

        // === Constructor

        protected SkillRole(string id, Getter getter) : base(enumName: id) {
            _getter = getter;
        }

        // === Static retrieval

        public static SkillRole DefaultForStat(StatType stat, Skill skill) {
            if (stat is ProfStatType) return Proficiency;
            if (stat is HeroStatType) return Hero;
            if (stat is CharacterStatType or AliveStatType) return Character;
            if (stat is MerchantStatType) return Merchant;
            if (stat is ItemStatType) return Item;
            
            throw new KeyNotFoundException($"Cannot determine a suitable default role for stat type: {stat} for skill {skill.Graph.name}");
        }

        // === Usage

        public T RetrieveFrom<T>(Skill skill) where T : class, IModel {
            return _getter(skill) as T;
        }

        public static Stat RetrieveStatFrom(Skill skill, StatType statType) {
            IWithStats model = DefaultForStat(statType, skill).RetrieveFrom<IWithStats>(skill);

            if (model == null || model.WasDiscarded) {
                return null;
            }

            return model.Stat(statType);
        }

    }
}
