using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats.Observers;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Heroes.Stats.Utils {
    /// <summary>
    /// Utilities for Proficiency data manipulation
    /// <seealso cref="ProfStatType"/>
    /// </summary>
    public static class ProfUtils {
        
        /// <summary>
        /// Read Proficiency Params from GameConstants
        /// </summary>
        public static ProficiencyParams[] ProfParams() => GameConstants.Get.proficiencyParams;

        /// <summary>
        /// Read Proficiency References to Abstract Templates from CommonReferences
        /// </summary>
        public static ProfAbstractRefs[] ProfReferences() => CommonReferences.Get.ProficiencyAbstractRefs;

        /// <summary>
        /// Get the proficiency related to an item
        /// </summary>
        [CanBeNull]
        public static ProfStatType ProfFromAbstracts(Item itemToSearch, bool suppressErrorLog = false) 
            => ProfFromAbstracts(itemToSearch.Template, ProfReferences(), suppressErrorLog);

        /// <summary>
        /// Get the proficiency related to an item
        /// </summary>
        [CanBeNull]
        public static ProfStatType ProfFromAbstracts(Item itemToSearch, IEnumerable<ProfAbstractRefs> proficiencyRelatedReferences, bool suppressErrorLog = false) {
            return ProfFromAbstracts(itemToSearch?.Template, proficiencyRelatedReferences, suppressErrorLog);
        }
        
        /// <summary>
        /// Get the proficiency related to an item
        /// </summary>
        [CanBeNull]
        public static ProfStatType ProfFromAbstracts(ItemTemplate itemTemplateToSearch, IEnumerable<ProfAbstractRefs> proficiencyRelatedReferences, bool suppressErrorLog = false) {
            if (itemTemplateToSearch == null) return ProfStatType.Unarmed;
            
            var profTempRef = proficiencyRelatedReferences.FirstOrDefault(reference => itemTemplateToSearch.InheritsFrom(reference.AbstractTemplate));
            
            if (profTempRef.AbstractTemplate == null || profTempRef.proficiency == null) {
                if (!suppressErrorLog) {
                    Log.Important?.Error("Attached Abstract is not a child of Proficiency Abstracts set in CommonReferences");
                }

                return null;
            }
            
            return profTempRef.proficiency.EnumAs<ProfStatType>();
        }

        /// <summary>
        /// Check the proficiency related to the currently equipped item in slot type
        /// </summary>
        [CanBeNull] [UnityEngine.Scripting.Preserve]
        public static ProfStatType CurrentlyEquippedProf(HeroItems heroItems, EquipmentSlotType slotToCheck) {
            return CurrentlyEquippedProf(heroItems, ProfReferences(), slotToCheck);
        }

        /// <summary>
        /// Check the proficiency related to the currently equipped item in slot type
        /// </summary>
        [CanBeNull]
        static ProfStatType CurrentlyEquippedProf(HeroItems heroItems, IEnumerable<ProfAbstractRefs> proficiencyRelatedReferences, EquipmentSlotType slotToCheck) {
            return ProfFromAbstracts(heroItems.EquippedItem(slotToCheck), proficiencyRelatedReferences);
        }
        
        /// <summary>
        /// Check the Proficiency value from the currently equipped item in item slot
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public static int CurrentlyEquippedStatLevel(Hero hero, EquipmentSlotType slotToCheck) 
            => CurrentlyEquippedStatLevel(hero, ProfReferences(), slotToCheck);

        /// <summary>
        /// Check the Proficiency value from the currently equipped item in item slot
        /// </summary>
        static int CurrentlyEquippedStatLevel(Hero hero, IEnumerable<ProfAbstractRefs> proficiencyRelatedReferences, EquipmentSlotType slotToCheck) {
            ProfStatType currentlyEquippedProficiency = CurrentlyEquippedProf(hero.HeroItems, proficiencyRelatedReferences, slotToCheck);
            return currentlyEquippedProficiency == null
                ? 0
                : currentlyEquippedProficiency.RetrieveFrom(hero).ModifiedInt;
        }
        
        /// <summary>
        /// Get the Parameters for provided Proficiency
        /// </summary>
        public static ProficiencyParams GetProfParams(ProfStatType profToFind)
            => GetProfParams(ProfParams(), profToFind);

        /// <summary>
        /// Get the Parameters for provided Proficiency
        /// </summary>
        static ProficiencyParams GetProfParams(IEnumerable<ProficiencyParams> list,
            ProfStatType profToFind) {
            return list.FirstOrDefault(x => x.proficiency == profToFind);
        }

        /// <summary>
        /// Get the effects of Proficiency, related to provided statToFind.
        /// </summary>
        public static IEnumerable<ProficiencyParams> GetProfParamsEffectingStat(StatType statToFind) 
            => GetProfParamsEffectingStat(ProfParams(), statToFind);
        
        /// <summary>
        /// Get the effects of Proficiency, related to provided statToFind
        /// Small optimization for non dynamic case where the Proficiency is known
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public static StatEffect GetProfParamsEffectingStat(ProfStatType profToSearchIn, StatType statToFind) {
            var effectsList = ProfParams().First(parameters => parameters.proficiency == profToSearchIn).Effects;
            return effectsList.First(effect => effect.StatEffected == statToFind);
        }
        
        static IEnumerable<ProficiencyParams> GetProfParamsEffectingStat(IEnumerable<ProficiencyParams> list,
            StatType statToFind) {
            
            return list.Where(x => x.Effects.Any(effects => effects.StatEffected == statToFind));
        }

        /// <summary>
        /// Get the total effect on stat from all proficiencies after level consideration. Warning: Not all stats should be handled this way!
        /// </summary>
        /// <param name="levelSource">The ICharacter to search the stats for</param>
        /// <param name="statType">The stat to search for</param>
        /// <returns></returns>
        [UnityEngine.Scripting.Preserve]
        public static float SumEffectOnStat(Hero levelSource, StatType statType) {
            IEnumerable<ProficiencyParams> proficiencyParams = GetProfParamsEffectingStat(statType);
            
            return proficiencyParams.Sum(profParam 
                => profParam.GetEffectStrOfType(statType)
                   * profParam.GetProficiencyLevel(levelSource));
        }

        public static ProfAbstractRefs GetProfAbstractRefs(ProfStatType profToFind) {
            return ProfReferences().FirstOrDefault(x => x.proficiency == profToFind);
        }
    }
}
