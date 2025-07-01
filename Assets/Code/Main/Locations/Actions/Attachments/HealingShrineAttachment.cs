using System;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Assets;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility.Times;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions.Attachments {
    [AttachesTo(typeof(LocationSpec), AttachmentCategory.Technical, "Used to set parameters in healing shrines that are used to heal player and activate again after time")]
    public class HealingShrineAttachment : MonoBehaviour, IAttachmentSpec {
        [SerializeField, FoldoutGroup("Healing")] bool healHealth = true;
        [SerializeField, FoldoutGroup("Healing")] bool healMana;
        [SerializeField, FoldoutGroup("Healing")] bool healStamina;
        [SerializeField, FoldoutGroup("Healing")] bool healWyrdSkill;
        [SerializeField] ARTimeSpan restoreTime = new ARTimeSpan(TimeSpan.TicksPerDay);
        [SerializeField, ARAssetReferenceSettings(new []{typeof(GameObject)}, group: AddressableGroup.VFX)]
        ShareableARAssetReference useEffectVFX;
        [SerializeField] DrakeAnimatedPropertiesOverrideController animatedPropertiesOverrideController;
        
        public bool HealHealth => healHealth;
        public bool HealMana => healMana;
        public bool HealStamina => healStamina;
        public bool HealWyrdSkill => healWyrdSkill;
        public ARTimeSpan RestoreTime => restoreTime;
        public ShareableARAssetReference UseEffectVFX => useEffectVFX;
        public DrakeAnimatedPropertiesOverrideController AnimatedPropertiesOverrideController => animatedPropertiesOverrideController;

        public Element SpawnElement() => new HealingShrineAction();
        public bool IsMine(Element element) => element is HealingShrineAction;
    }
}