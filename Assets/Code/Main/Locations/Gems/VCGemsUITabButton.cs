using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    public class VCGemsUITabButton : GemsUITabs.VCHeaderTabButton {
        [RichEnumExtends(typeof(GemsUITabType))] 
        [SerializeField] RichEnumReference tabType;

        public override GemsUITabType Type => tabType.EnumAs<GemsUITabType>();
        public override string ButtonName => Type.Title;
    }
}