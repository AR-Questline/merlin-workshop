using Awaken.Utility;
using System;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Utility.VFX {
    [Serializable]
    public partial class ItemVfxContainer {
        public ushort TypeForSerialization => SavedTypes.ItemVfxContainer;

        [Saved, ListDrawerSettings(IsReadOnly = true), LabelText("When Metal Hits Surface:")]
        public VfxByHitSurface[] damageTypeMetalVfx = {
            new(SurfaceType.HitWood, SurfaceType.DamageWood),
            new(SurfaceType.HitStone),
            new(SurfaceType.HitMetal, SurfaceType.ArmorMetal, SurfaceType.DamageMetal),
            new(SurfaceType.HitFlesh, SurfaceType.DamageOrganic),
            new(SurfaceType.HitGround),
            new(SurfaceType.HitMagic, SurfaceType.DamageMagic),
            new(SurfaceType.HitFabric, SurfaceType.ArmorFabric),
            new(SurfaceType.HitLeather, SurfaceType.ArmorLeather),
            new(SurfaceType.HitBones),
        };

        [Saved, ListDrawerSettings(IsReadOnly = true), LabelText("When Wood Hits Surface:")]
        public VfxByHitSurface[] damageTypeWoodVfx = {
            new(SurfaceType.HitWood, SurfaceType.DamageWood),
            new(SurfaceType.HitStone),
            new(SurfaceType.HitMetal, SurfaceType.ArmorMetal, SurfaceType.DamageMetal),
            new(SurfaceType.HitFlesh, SurfaceType.DamageOrganic),
            new(SurfaceType.HitGround),
            new(SurfaceType.HitMagic, SurfaceType.DamageMagic),
            new(SurfaceType.HitFabric, SurfaceType.ArmorFabric),
            new(SurfaceType.HitLeather, SurfaceType.ArmorLeather),
            new(SurfaceType.HitBones),
        };

        [Saved, ListDrawerSettings(IsReadOnly = true), LabelText("When Arrow Hits Surface:")]
        public VfxByHitSurface[] damageTypeArrowVfx = {
            new(SurfaceType.HitWood, SurfaceType.DamageWood),
            new(SurfaceType.HitStone),
            new(SurfaceType.HitMetal, SurfaceType.ArmorMetal, SurfaceType.DamageMetal),
            new(SurfaceType.HitFlesh, SurfaceType.DamageOrganic),
            new(SurfaceType.HitGround),
            new(SurfaceType.HitMagic, SurfaceType.DamageMagic),
            new(SurfaceType.HitFabric, SurfaceType.ArmorFabric),
            new(SurfaceType.HitLeather, SurfaceType.ArmorLeather),
            new(SurfaceType.HitBones),
        };

        [Saved, ListDrawerSettings(IsReadOnly = true), LabelText("When Magic Hits Surface:")]
        public VfxByHitSurface[] damageTypeMagicVfx = {
            new(SurfaceType.HitWood, SurfaceType.DamageWood),
            new(SurfaceType.HitStone),
            new(SurfaceType.HitMetal, SurfaceType.ArmorMetal, SurfaceType.DamageMetal),
            new(SurfaceType.HitFlesh, SurfaceType.DamageOrganic),
            new(SurfaceType.HitGround),
            new(SurfaceType.HitMagic, SurfaceType.DamageMagic),
            new(SurfaceType.HitFabric, SurfaceType.ArmorFabric),
            new(SurfaceType.HitLeather, SurfaceType.ArmorLeather),
            new(SurfaceType.HitBones),
        };

        [Saved, ListDrawerSettings(IsReadOnly = true), LabelText("When Organic(Fists) Hits Surface:")]
        public VfxByHitSurface[] damageTypeOrganicVfx = {
            new(SurfaceType.HitWood, SurfaceType.DamageWood),
            new(SurfaceType.HitStone),
            new(SurfaceType.HitMetal, SurfaceType.ArmorMetal, SurfaceType.DamageMetal),
            new(SurfaceType.HitFlesh, SurfaceType.DamageOrganic),
            new(SurfaceType.HitGround),
            new(SurfaceType.HitMagic, SurfaceType.DamageMagic),
            new(SurfaceType.HitFabric, SurfaceType.ArmorFabric),
            new(SurfaceType.HitLeather, SurfaceType.ArmorLeather),
            new(SurfaceType.HitBones),
        };

        public ShareableARAssetReference GetVFX(SurfaceType damageType, SurfaceType hitSurface) {
            if (damageType == SurfaceType.DamageMetal) {
                return damageTypeMetalVfx.FirstOrDefault(v => v.HitSurfaces.Contains(hitSurface)).vfxEffect;
            } else if (damageType == SurfaceType.DamageWood) {
                return damageTypeWoodVfx.FirstOrDefault(v => v.HitSurfaces.Contains(hitSurface)).vfxEffect;
            } else if (damageType == SurfaceType.DamageArrow) {
                return damageTypeArrowVfx.FirstOrDefault(v => v.HitSurfaces.Contains(hitSurface)).vfxEffect;
            } else if (damageType == SurfaceType.DamageMagic) {
                return damageTypeMagicVfx.FirstOrDefault(v => v.HitSurfaces.Contains(hitSurface)).vfxEffect;
            } else if (damageType == SurfaceType.DamageOrganic) {
                return damageTypeOrganicVfx.FirstOrDefault(v => v.HitSurfaces.Contains(hitSurface)).vfxEffect;
            }

            return null;
        }
    }
}
