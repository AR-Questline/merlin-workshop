using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC.Elements;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Attachment {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Common, "Makes the location killable, f.e. destroyable barrels.")]
    public class AliveLocationAttachment : MonoBehaviour, IAttachmentSpec, AliveStats.ITemplate {
        public int maxHealth = 100;
        public float healthRegen;
        public bool discardOnDeath;
        [ShowIf(nameof(discardOnDeath)), Range(0, 10)]
        public float discardDelayInSeconds;

        [Tooltip("Armor is multiplied by this before being returned as " + nameof(ICharacter.TotalArmor) + " for calculations")]
        public int armorMultiplier = 1;
        public int armor = 0;
        
        [UnityEngine.Scripting.Preserve] public float forceModifier = 1;
        [RichEnumExtends(typeof(SurfaceType))] 
        public RichEnumReference surfaceType = SurfaceType.HitFlesh;
        [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true)]
        public ShareableARAssetReference hitVFXReference;
        [ARAssetReferenceSettings(new[] {typeof(GameObject)}, true)]
        public ShareableARAssetReference deathVFXReference;
        
        [SerializeField] StoryBookmark storyOnDeath;
        
        [FoldoutGroup("Combat Stats"), ListDrawerSettings(CustomAddFunction = nameof(AddDefaultDamageReceivedMultiplier)), SerializeField]
        List<DamageReceivedMultiplierDataConfig> damageReceivedMultipliers = new ();
        
        public int MaxHealth => maxHealth;
        [UnityEngine.Scripting.Preserve] public float HealthRegen => healthRegen;
        public float ArmorMultiplier => armorMultiplier;
        public int Armor => armor;
        public float StatusResistance => 0f;
        public float ForceStumbleThreshold => 0f;
        public float TrapDamageMultiplier => 1f;
        
        public StoryBookmark StoryOnDeath => storyOnDeath;

        public Element SpawnElement() => new AliveLocation();
        public bool IsMine(Element element) => element is AliveLocation;

        public DamageReceivedMultiplierData GetDamageReceivedMultiplierData() {
            if (damageReceivedMultipliers is not { Count: > 0 }) {
                return null;
            }
            var parts = new DamageReceivedMultiplierDataPart[damageReceivedMultipliers.Count];
            for (int i = 0; i < damageReceivedMultipliers.Count; i++) {
                parts[i] = DamageReceivedMultiplierDataConfig.Construct(damageReceivedMultipliers[i]);
            }
            return new DamageReceivedMultiplierData(parts);
        }
        
        DamageReceivedMultiplierDataConfig AddDefaultDamageReceivedMultiplier() => DamageReceivedMultiplierDataConfig.Default;
    }
}
