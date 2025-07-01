using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Character {
    public class VCInventoryTabButton : CharacterSubTabs.VCHeaderTabButton {
        [RichEnumExtends(typeof(CharacterSubTabType))] 
        [SerializeField] RichEnumReference tabType;
        public override CharacterSubTabType Type => tabType.EnumAs<CharacterSubTabType>();
        public override string ButtonName => Type.Title;
    }
}