using System;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using FMODUnity;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.Attachments.Audio {
    [Serializable]
    public partial class ItemAudioContainer {
        public ushort TypeForSerialization => SavedTypes.ItemAudioContainer;

        [Saved] public AudioType audioType;
        [Saved, RichEnumExtends(typeof(SurfaceType)), SerializeField] [ShowIfGroup(nameof(IsArmor))] 
        RichEnumReference armorHitType = SurfaceType.HitLeather;
        
        [Saved, FoldoutGroup("IsMelee/Melee")] [ShowIfGroup(nameof(IsMelee)), LabelWidth(150)] 
        public EventReference meleeSwing, meleeSwingHeavy, meleeDashAttack, meleeEquip, meleeUnEquip, meleeHit, pommelHit;

        [Saved, FoldoutGroup("IsBow/Bow")] [ShowIfGroup(nameof(IsBow)), LabelWidth(100)] 
        public EventReference dragBow, equipBow, unEquipBow, releaseBow, arrowSwish;

        [Saved, FoldoutGroup("IsMagic/Magic")] [ShowIfGroup(nameof(IsMagic)), LabelWidth(160)] 
        public EventReference castBegun, castCharging, castFullyCharged, castCancel, castRelease, castHeavyRelease, 
            equipMagic, unEquipMagic, magicHeldIdle, magicHeldChargedIdle, projectileIdle, magicHit, magicFailedCast;
        
        [Saved, FoldoutGroup("Weapon")] [ShowIf(nameof(IsWeapon)), LabelWidth(100)]
        public EventReference sheathe, unsheathe;
        
        [Saved, FoldoutGroup("IsShield/Blocking")] [ShowIfGroup(nameof(IsShield)), LabelWidth(150)] 
        public EventReference onBlockDamage, parrySwing, pommelSwing;
        
        [Saved, FoldoutGroup("IsArmor/Armor")] [ShowIfGroup(nameof(IsArmor)), LabelWidth(150)] 
        public EventReference footStep, bodyMovement, bodyMovementFast, equipArmor, unEquipArmor;

        [Saved, FoldoutGroup("General")] [ShowIf(nameof(IsUseAble)), LabelWidth(100)] public EventReference useItem;
        [Saved, FoldoutGroup("General"), LabelWidth(100)] public EventReference pickupItem;
        [Saved, FoldoutGroup("General"), LabelWidth(100)] public EventReference dropItem;
        
        [Saved, FoldoutGroup("Special")] [LabelWidth(150)] 
        public EventReference specialAttackSwing;

        bool IsMelee => audioType.HasFlagFast(AudioType.Melee);
        bool IsBow => audioType.HasFlagFast(AudioType.Bow);
        bool IsMagic => audioType.HasFlagFast(AudioType.Magic);
        bool IsWeapon => IsMelee || IsBow || IsMagic;
        bool IsUseAble => audioType.HasFlagFast(AudioType.UseAble);
        bool IsShield => audioType.HasFlagFast(AudioType.Shield) || audioType.HasFlagFast(AudioType.Melee);
        bool IsArmor => audioType.HasFlagFast(AudioType.Armor);
        public SurfaceType ArmorHitType => armorHitType.EnumAs<SurfaceType>();

        [Flags]
        public enum AudioType {
            Melee = 1 << 1,
            Bow = 1 << 2,
            Magic = 1 << 3,
            UseAble = 1 << 4,
            Shield = 1 << 5,
            Armor = 1 << 6,
            [UnityEngine.Scripting.Preserve] All = int.MaxValue
        }
    }
}