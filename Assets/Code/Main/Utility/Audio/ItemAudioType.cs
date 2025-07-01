using System;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments.Audio;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using FMODUnity;

namespace Awaken.TG.Main.Utility.Audio {
    [RichEnumAlwaysDisplayCategory]
    public class ItemAudioType : ARAudioType<Item> {
        protected ItemAudioType(string id, Func<Item, EventReference> getter, string inspectorCategory = "") : base(id, getter, inspectorCategory) { }

        [UnityEngine.Scripting.Preserve]
        public static readonly ItemAudioType
            MeleeSwing = new(nameof(MeleeSwing), i => GetAudioContainer(i).meleeSwing, "Melee"),
            MeleeSwingHeavy = new(nameof(MeleeSwingHeavy), i => GetAudioContainer(i).meleeSwingHeavy, "Melee"),
            MeleeDashAttack = new(nameof(MeleeDashAttack), i => GetAudioContainer(i).meleeDashAttack, "Melee"),
            MeleeEquip = new(nameof(MeleeEquip), i => GetAudioContainer(i).meleeEquip, "Melee"),
            MeleeUnEquip = new(nameof(MeleeUnEquip), i => GetAudioContainer(i).meleeUnEquip, "Melee"),
            MeleeHit = new(nameof(MeleeHit), i => GetAudioContainer(i).meleeHit, "Melee"),
            PommelHit = new(nameof(PommelHit), i => GetAudioContainer(i).pommelHit, "Melee"),
            DragBow = new(nameof(DragBow), i => GetAudioContainer(i).dragBow, "Bow"),
            EquipBow = new(nameof(EquipBow), i => GetAudioContainer(i).equipBow, "Bow"),
            UnEquipBow = new(nameof(UnEquipBow), i => GetAudioContainer(i).unEquipBow, "Bow"),
            ReleaseBow = new(nameof(ReleaseBow), i => GetAudioContainer(i).releaseBow, "Bow"),
            ArrowSwish = new(nameof(ArrowSwish), i => GetAudioContainer(i).arrowSwish, "Bow"),
            CastBegun = new(nameof(CastBegun), i => GetAudioContainer(i).castBegun, "Magic"),
            CastCharging = new(nameof(CastCharging), i => GetAudioContainer(i).castCharging, "Magic"),
            CastFullyCharged = new(nameof(CastFullyCharged), i => GetAudioContainer(i).castFullyCharged, "Magic"),
            CastCancel = new(nameof(CastCancel), i => GetAudioContainer(i).castCancel, "Magic"),
            CastRelease = new(nameof(CastRelease), i => GetAudioContainer(i).castRelease, "Magic"),
            CastHeavyRelease = new(nameof(CastHeavyRelease), i => GetAudioContainer(i).castHeavyRelease, "Magic"),
            FailedCast = new(nameof(FailedCast), i => GetAudioContainer(i).magicFailedCast, "Magic"),
            EquipMagic = new(nameof(EquipMagic), i => GetAudioContainer(i).equipMagic, "Magic"),
            UnEquipMagic = new(nameof(UnEquipMagic), i => GetAudioContainer(i).unEquipMagic, "Magic"),
            MagicHeldIdle = new(nameof(MagicHeldIdle), i => GetAudioContainer(i).magicHeldIdle, "Magic"),
            MagicHeldChargedIdle = new(nameof(MagicHeldChargedIdle), i => GetAudioContainer(i).magicHeldChargedIdle, "Magic"),
            ProjectileIdle = new(nameof(ProjectileIdle), i => GetAudioContainer(i).projectileIdle, "Magic"),
            MagicHit = new(nameof(MagicHit), i => GetAudioContainer(i).magicHit, "Magic"),
            Sheathe = new(nameof(Sheathe), i => GetAudioContainer(i).sheathe, "Weapon"),
            Unsheathe = new(nameof(Unsheathe), i => GetAudioContainer(i).unsheathe, "Weapon"),
            UseItem = new(nameof(UseItem), i => GetAudioContainer(i).useItem, "Item"),
            PickupItem = new(nameof(PickupItem), i => GetAudioContainer(i).pickupItem, "Item"),
            DropItem = new(nameof(DropItem), i => GetAudioContainer(i).dropItem, "Item"),
            BlockDamage = new(nameof(BlockDamage), i => GetAudioContainer(i).onBlockDamage, "Block"),
            ParrySwing = new(nameof(ParrySwing), i => GetAudioContainer(i).parrySwing, "Block"),
            PommelSwing = new(nameof(PommelSwing), i => GetAudioContainer(i).pommelSwing, "Block"),
            SpecialAttackSwing = new(nameof(SpecialAttackSwing), i => GetAudioContainer(i).specialAttackSwing, "Special");

        static ItemAudioContainer GetAudioContainer(Item item) => item?.TryGetElement<ItemAudio>()?.AudioContainer ?? CommonReferences.Get.AudioConfig.DefaultItemAudioContainer;
    }
}