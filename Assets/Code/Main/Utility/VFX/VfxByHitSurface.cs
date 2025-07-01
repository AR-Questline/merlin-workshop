using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Utility.VFX {
    [Serializable]
    public partial struct VfxByHitSurface {
        public ushort TypeForSerialization => SavedTypes.VfxByHitSurface;

        [Saved, RichEnumExtends(typeof(SurfaceType)), ReadOnly, SerializeField]
        RichEnumReference hitSurface;
        [Saved, SerializeField, ReadOnly]
        RichEnumReference[] additionalSurfaces;

        [Saved, ARAssetReferenceSettings(new[] {typeof(GameObject)}, true, AddressableGroup.VFX)]
        public ShareableARAssetReference vfxEffect;

        public IEnumerable<SurfaceType> HitSurfaces {
            get {
                foreach (var s in additionalSurfaces) {
                    yield return s.EnumAs<SurfaceType>();
                }
                yield return hitSurface.EnumAs<SurfaceType>();
            }
        }

        public VfxByHitSurface(SurfaceType hitSurface, params SurfaceType[] additionalSurfaces) {
            this.hitSurface = hitSurface;
            this.additionalSurfaces = additionalSurfaces.Select(s => (RichEnumReference)s).ToArray();
            vfxEffect = null;
        }
    }
}