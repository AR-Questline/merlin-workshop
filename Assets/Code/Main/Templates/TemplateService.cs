using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Templates {
    [Searchable]
    public class TemplateService : MonoBehaviour, IService {
        // === Common
        
        [FoldoutGroup("Common")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractVeryLightWeight;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractVeryLightWeight => _abstractVeryLightWeight.Get<ItemTemplate>();
        
        [FoldoutGroup("Common")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractLightWeight;
        public ItemTemplate AbstractLightWeight => _abstractLightWeight.Get<ItemTemplate>();
        
        [FoldoutGroup("Common")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractMediumWeight;
        public ItemTemplate AbstractMediumWeight => _abstractMediumWeight.Get<ItemTemplate>();
        
        [FoldoutGroup("Common")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractHeavyWeight;
        public ItemTemplate AbstractHeavyWeight => _abstractHeavyWeight.Get<ItemTemplate>();
        
        [FoldoutGroup("Common")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractVeryHeavyWeight;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractVeryHeavyWeight => _abstractVeryHeavyWeight.Get<ItemTemplate>();
        
        // === Weapons
        
        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeapon;
        public ItemTemplate AbstractWeapon => _abstractWeapon.Get<ItemTemplate>();
        
        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeaponMelee;
        public ItemTemplate AbstractWeaponMelee => _abstractWeaponMelee.Get<ItemTemplate>();
        
        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeapon1H;
        public ItemTemplate AbstractWeapon1H => _abstractWeapon1H.Get<ItemTemplate>();
        
        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeapon2H;
        public ItemTemplate AbstractWeapon2H => _abstractWeapon2H.Get<ItemTemplate>();

        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeaponDefaultFists;
        public ItemTemplate AbstractWeaponDefaultFists => _abstractWeaponDefaultFists.Get<ItemTemplate>();

        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeaponFists;
        public ItemTemplate AbstractWeaponFists => _abstractWeaponFists.Get<ItemTemplate>();

        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeaponDagger;
        public ItemTemplate AbstractWeaponDagger => _abstractWeaponDagger.Get<ItemTemplate>();

        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeaponAxe;
        public ItemTemplate AbstractWeaponAxe => _abstractWeaponAxe.Get<ItemTemplate>();

        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeaponSword;
        public ItemTemplate AbstractWeaponSword => _abstractWeaponSword.Get<ItemTemplate>();

        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeaponBlunt;
        public ItemTemplate AbstractWeaponBlunt => _abstractWeaponBlunt.Get<ItemTemplate>();

        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeaponPolearm;
        public ItemTemplate AbstractWeaponPolearm => _abstractWeaponPolearm.Get<ItemTemplate>();

        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractShield;
        public ItemTemplate AbstractShield => _abstractShield.Get<ItemTemplate>();
        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractRod;
        public ItemTemplate AbstractRod => _abstractRod.Get<ItemTemplate>();

        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractWeaponRanged;
        public ItemTemplate AbstractWeaponRanged => _abstractWeaponRanged.Get<ItemTemplate>();

        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractArrow;
        public ItemTemplate AbstractArrow => _abstractArrow.Get<ItemTemplate>();
        
        [FoldoutGroup("Weapons")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractThrowable;
        public ItemTemplate AbstractThrowable => _abstractThrowable.Get<ItemTemplate>();
        
        [FoldoutGroup("Magic")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractMagic;
        public ItemTemplate AbstractMagic => _abstractMagic.Get<ItemTemplate>();
        
        [FoldoutGroup("Magic")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractMagicCastArtillery;
        public ItemTemplate AbstractMagicCastArtillery => _abstractMagicCastArtillery.Get<ItemTemplate>();
        
        [FoldoutGroup("Magic")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractMagicCastChaingun;
        public ItemTemplate AbstractMagicCastChaingun => _abstractMagicCastChaingun.Get<ItemTemplate>();
        
        [FoldoutGroup("Magic")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractMagicCastPistol;
        public ItemTemplate AbstractMagicCastPistol => _abstractMagicCastPistol.Get<ItemTemplate>();
        
        [FoldoutGroup("Magic")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractMagicCastRailgun;
        public ItemTemplate AbstractMagicCastRailgun => _abstractMagicCastRailgun.Get<ItemTemplate>();
        
        [FoldoutGroup("Magic")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractMagicCastRocketLauncher;
        public ItemTemplate AbstractMagicCastRocketLauncher => _abstractMagicCastRocketLauncher.Get<ItemTemplate>();
        
        [FoldoutGroup("Magic")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractMagicCastShotgun;
        public ItemTemplate AbstractMagicCastShotgun => _abstractMagicCastShotgun.Get<ItemTemplate>();
        
        [FoldoutGroup("Magic")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractSoulCube;
        public ItemTemplate AbstractSoulCube => _abstractSoulCube.Get<ItemTemplate>();

        // === Armors
        
        [FoldoutGroup("Armors")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractArmor;
        public ItemTemplate AbstractArmor => _abstractArmor.Get<ItemTemplate>();
        
        [FoldoutGroup("Armors")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractHelmet;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractHelmet => _abstractHelmet.Get<ItemTemplate>();
        
        [FoldoutGroup("Armors")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractCuirass;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractCuirass => _abstractCuirass.Get<ItemTemplate>();
        
        [FoldoutGroup("Armors")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractGauntlets;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractGauntlets => _abstractGauntlets.Get<ItemTemplate>();
        
        [FoldoutGroup("Armors")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractGreaves;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractGreaves => _abstractGreaves.Get<ItemTemplate>();
        
        [FoldoutGroup("Armors")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractBoots;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractBoots => _abstractBoots.Get<ItemTemplate>();
        
        // === Crafting
        
        [FoldoutGroup("Crafting")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractMetal;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractMetal => _abstractMetal.Get<ItemTemplate>();
        
        [FoldoutGroup("Crafting")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractComponent;
        public ItemTemplate AbstractComponent => _abstractComponent.Get<ItemTemplate>();
        
        [FoldoutGroup("Crafting")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractJewelry;
        public ItemTemplate AbstractJewelry => _abstractJewelry.Get<ItemTemplate>();
        
        // === Consumables
        [FoldoutGroup("Consumables")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractConsumableHealth;
        public ItemTemplate AbstractConsumableHealth => _abstractConsumableHealth.Get<ItemTemplate>();
        
        [FoldoutGroup("Consumables")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractConsumableMana;
        public ItemTemplate AbstractConsumableMana => _abstractConsumableMana.Get<ItemTemplate>();
        
        [FoldoutGroup("Consumables")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractConsumableStamina;
        public ItemTemplate AbstractConsumableStamina => _abstractConsumableStamina.Get<ItemTemplate>();
        
        [FoldoutGroup("Consumables")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractConsumablePotionOther;
        public ItemTemplate AbstractConsumablePotionOther => _abstractConsumablePotionOther.Get<ItemTemplate>();
        
        [FoldoutGroup("Consumables")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractConsumableStat;
        public ItemTemplate AbstractConsumableStat => _abstractConsumableStat.Get<ItemTemplate>();
        
        [FoldoutGroup("Consumables")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractConsumable;
        public ItemTemplate AbstractConsumable => _abstractConsumable.Get<ItemTemplate>();
        
        [FoldoutGroup("Consumables")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractPotion;
        public ItemTemplate AbstractPotion => _abstractPotion.Get<ItemTemplate>();
        
        [FoldoutGroup("Consumables")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractPlainFood;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractPlainFood => _abstractPlainFood.Get<ItemTemplate>();
        
        [FoldoutGroup("Consumables")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractAlcohol;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractAlcohol => _abstractAlcohol.Get<ItemTemplate>();
        
        // === Misc
        
        [FoldoutGroup("Misc")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractKey;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractKey => _abstractKey.Get<ItemTemplate>();
        
        [FoldoutGroup("Misc")] [SerializeField] [TemplateType(typeof(ItemTemplate))]
        TemplateReference _abstractTool;
        [UnityEngine.Scripting.Preserve] public ItemTemplate AbstractTool => _abstractTool.Get<ItemTemplate>();
        
        // === NpcTemplates
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractPreyAnimal;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractPreyAnimal => _abstractPreyAnimal.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractAnimal;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractAnimal => _abstractPreyAnimal.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractBandit;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractBandit => _abstractBandit.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractBigHumanoid;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractBigHumanoid => _abstractBigHumanoid.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractBloody;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractBloody => _abstractBloody.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractBoneMask;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractBoneMask => _abstractBoneMask.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractBoss;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractBoss => _abstractBoss.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractCultist;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractCultist => _abstractCultist.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractDalRiataBody;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractDalRiataBody => _abstractDalRiataBody.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractFemale;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractFemale => _abstractFemale.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractGhost;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractGhost => _abstractGhost.Get<NpcTemplate>();

        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractHumanoid;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractHumanoid => _abstractHumanoid.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractMale;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractMale => _abstractMale.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractMiniBoss;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractMiniBoss => _abstractMiniBoss.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractMonster;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractMonster => _abstractMonster.Get<NpcTemplate>();
        
        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractSkeleton;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractSkeleton => _abstractSkeleton.Get<NpcTemplate>();

        [FoldoutGroup("Npc")] [SerializeField] [TemplateType(typeof(NpcTemplate))]
        TemplateReference _abstractZombie;
        [UnityEngine.Scripting.Preserve] public NpcTemplate AbstractZombie => _abstractZombie.Get<NpcTemplate>();

        public bool IsAnimal(NpcTemplate template) {
            return template.InheritsFrom(_abstractPreyAnimal.Get<NpcTemplate>()) ||
                   template.InheritsFrom(_abstractAnimal.Get<NpcTemplate>());
        }
        
        public bool IsMonster(NpcTemplate template) {
            return template.InheritsFrom(_abstractBigHumanoid.Get<NpcTemplate>()) ||
                   template.InheritsFrom(_abstractBloody.Get<NpcTemplate>()) ||
                   template.InheritsFrom(_abstractBoneMask.Get<NpcTemplate>()) ||
                   template.InheritsFrom(_abstractBoss.Get<NpcTemplate>()) ||
                   template.InheritsFrom(_abstractGhost.Get<NpcTemplate>()) ||
                   template.InheritsFrom(_abstractCultist.Get<NpcTemplate>()) ||
                   template.InheritsFrom(_abstractDalRiataBody.Get<NpcTemplate>()) || //??
                   template.InheritsFrom(_abstractMiniBoss.Get<NpcTemplate>()) ||
                   template.InheritsFrom(_abstractMonster.Get<NpcTemplate>()) ||
                   template.InheritsFrom(_abstractSkeleton.Get<NpcTemplate>()) ||
                   template.InheritsFrom(_abstractZombie.Get<NpcTemplate>());
        }
        
        public bool IsNonMagicWeaponOrArrow(ItemTemplate template) {
            return (template.InheritsFrom(_abstractWeapon.Get<ItemTemplate>()) ||
                   template.InheritsFrom(_abstractArrow.Get<ItemTemplate>())) && 
                   template.InheritsFrom(_abstractMagic.Get<ItemTemplate>()) == false;

        }
    }
}