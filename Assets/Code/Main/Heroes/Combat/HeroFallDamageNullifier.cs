using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    public class HeroFallDamageNullifier : MonoBehaviour {
        [SerializeField, RichEnumExtends(typeof(SurfaceType), new[] {"FallNullifier"}, true)]
        RichEnumReference surfaceType;

        public SurfaceType SurfaceType => surfaceType.EnumAs<SurfaceType>();
    }
}